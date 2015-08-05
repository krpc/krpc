#!/usr/bin/env python

# Build API docs

import sys
import os
import re
import json
import xml.etree.ElementTree as ElementTree
from Cheetah.Template import Template
from krpc.attributes import Attributes
from krpc.types import Types
from api.directives import *
import api.utils

language = sys.argv[1]
src = sys.argv[2]
dst = sys.argv[3]

class Env(object):

    def __init__(self, language):
        self.types = Types()
        self._refs = {}
        self._currentmodule = None

        if language == 'python':
            from api.python import PythonAPI
            self._domain = PythonAPI(self)
        elif language == 'lua':
            from api.lua import LuaAPI
            self._domain = LuaAPI(self)
        else:
            raise RuntimeError('Unknown language')

        self._member_ordering_seen = set()
        with open('order.txt', 'r') as f:
            self._member_ordering = [x.strip() for x in f.readlines()]

    @property
    def domain(self):
        return self._domain

    @domain.setter
    def domain(self, domain):
        self._domain = domain

    def currentmodule(self, name):
        self._currentmodule = name
        return '.. currentmodule:: %s\n' % name

    def add_ref(self, name, obj):
        self._refs[name] = obj

    def get_ref(self, name):
        if name not in self._refs:
            raise RuntimeError('Ref not found %s' % name)
        return self._refs[name]

    def shorten_ref(self, name):
        if self._currentmodule and name.startswith(self._currentmodule+'.'):
            return name[len(self._currentmodule)+1:]
        return name

    def sorted_members(self, members):
        def key_fn(x):
            if x.name not in self._member_ordering:
                print 'Don\'t know how to order member', x.name
                return float('inf')
            self._member_ordering_seen.add(x.name)
            return self._member_ordering.index(x.name)
        return sorted(members, key=key_fn)

    def check_seen_members(self):
        unseen = set(self._member_ordering).difference(self._member_ordering_seen)
        if len(unseen) > 0:
            print 'WARNING: unseen members in order list:'
            for x in unseen:
                print '   ', x

    def parse_documentation(self, xml, info=None):
        if xml.strip() == '':
            return '', []
        parser = ElementTree.XMLParser(encoding='UTF-8')
        root = ElementTree.XML(xml.encode('UTF-8'), parser=parser)
        description = ''
        objs = []
        for node in root:
            if node.tag == 'summary':
                description = self.parse_description(node)
            elif node.tag == 'param':
                name = node.attrib['name']
                desc = self.parse_description(node)
                pinfo = None
                for parameter in info['parameters']:
                    if parameter['name'] == name:
                        pinfo = parameter
                pos = filter(lambda x: x[1]['name'] == name, enumerate(info['parameters']))[0][0]
                objs.append(Param(self, name, desc, pos, pinfo, info['attributes']))
            elif node.tag == 'returns':
                objs.append(Returns(self, self.parse_description(node)))
            elif node.tag == 'remarks':
                objs.append(Note(self, self.parse_description(node)))
            else:
                raise RuntimeError('Unhandled documentation tag type %s' % node.tag)
        return description, objs

    def parse_description_node(self, node):
        if node.tag == 'see':
            return env.domain.parse_ref(node.attrib['cref'])
        elif node.tag == 'paramref':
            return '*%s*' % env.domain.parse_param(node.attrib['name'])
        elif node.tag == 'a':
            return '`%s <%s>`_' % (node.text.replace('\n',''), node.attrib['href'])
        elif node.tag == 'c':
            return '``%s``' % env.domain.parse_value(node.text)
        elif node.tag == 'math':
            return ':math:`%s`' % node.text
        elif node.tag == 'list':
            content = '\n'
            for item in node:
                item_content = self.parse_description(item[0])
                content += '* %s\n' % '\n'.join(api.utils.indent(item_content.split('\n'), 2))[2:].rstrip()
            return content
        else:
            raise RuntimeError('Unhandled node type %s' % node.tag)

    def parse_description(self, node):
        desc = node.text
        for child in node:
            desc += self.parse_description_node(child)
            if child.tail:
                desc += child.tail
        return desc.strip()

    def parse_parameter_type(self, pos, typ, attrs):
        typ = self.types.get_parameter_type(pos, typ, attrs)
        return self.domain.parse_type_ref(typ.protobuf_type)

    def parse_return_type(self, typ, attrs):
        typ = self.types.get_return_type(typ, attrs)
        return self.domain.parse_type(typ.protobuf_type)

env = Env(language)

with open('services.json', 'r') as f:
    services_info = json.load(f)

def process_file(path):

    services = {}
    classes = {}
    enumerations = {}

    for service_name,service_info in services_info.items():

        procedures = {}
        properties = {}
        for procedure_name,procedure_info in service_info['procedures'].items():
            if Attributes.is_a_procedure(procedure_info['attributes']):
                procedures[procedure_name] = StaticMethod(env, service_name, procedure_name, procedure_info)
            elif Attributes.is_a_property_accessor(procedure_info['attributes']):
                name = Attributes.get_property_name(procedure_info['attributes'])
                if name not in properties:
                    properties[name] = []
                properties[name].append(Property(env, service_name, name, procedure_info))

        members = []

        for name,procedure in procedures.items():
            env.add_ref('M:%s.%s' % (service_name, name), procedure)
            members.append(procedure)

        for name,props in properties.items():
            def merge_properties(props):
                result = props[0]
                for prop in props[1:]:
                    result.merge(prop)
                return result
            prop = merge_properties(props)
            env.add_ref('M:%s.%s' % (service_name, name), prop)
            members.append(prop)

        service = Service(env, service_name, service_info['documentation'], members)
        services[service_name] = service
        env.add_ref('T:%s' % service_name, service)

        for class_name,class_info in service_info['classes'].items():

            methods = {}
            properties = {}

            for procedure_name,procedure_info in service_info['procedures'].items():
                if Attributes.is_a_class_member(procedure_info['attributes']) and \
                   Attributes.get_class_name(procedure_info['attributes']) == class_name:
                    if Attributes.is_a_class_method(procedure_info['attributes']):
                        name = Attributes.get_class_method_name(procedure_info['attributes'])
                        methods[name] = ClassMethod(env, service_name, class_name, name, procedure_info)
                    elif Attributes.is_a_class_static_method(procedure_info['attributes']):
                        name = Attributes.get_class_method_name(procedure_info['attributes'])
                        methods[name] = ClassStaticMethod(env, service_name, class_name, name, procedure_info)
                    elif Attributes.is_a_class_property_accessor(procedure_info['attributes']):
                        name = Attributes.get_class_property_name(procedure_info['attributes'])
                        if name not in properties:
                            properties[name] = []
                        properties[name].append(ClassProperty(env, service_name, class_name, name, procedure_info))

            members = []

            for name,method in methods.items():
                env.add_ref('M:%s.%s.%s' % (service_name, class_name, name), method)
                members.append(method)

            for name,props in properties.items():
                prop = merge_properties(props)
                env.add_ref('M:%s.%s.%s' % (service_name, class_name, name), prop)
                members.append(prop)

            name = 'T:%s.%s' % (service_name, class_name)
            cls = Class(env, service_name, class_name, class_info['documentation'], members)
            classes[name[2:]] = cls
            env.add_ref(name, cls)

        for enum_name,enum_info in service_info['enumerations'].items():
            values = []
            for value in enum_info['values']:
                enum_value = EnumerationValue(env, service_name, enum_name, value['name'], value['documentation'])
                env.add_ref('M:%s.%s.%s' % (service_name, enum_name, value['name']), enum_value)
                values.append(enum_value)
            name = 'T:%s.%s' % (service_name, enum_name)
            enum = Enumeration(env, service_name, enum_name, enum_info['documentation'], values)
            enumerations[name[2:]] = enum
            env.add_ref(name, enum)

    namespace = {
        'language': language,
        'domain': env.domain.name,
        'services': services,
        'classes': classes,
        'enumerations': enumerations,
        'ref': env.domain.parse_ref,
        'value': env.domain.parse_value,
        'currentmodule': env.currentmodule
    }
    global currentmodule
    currentmodule = None
    template = Template(file=path, searchList=[namespace])
    result = str(template)
    currentmodule = None
    env.check_seen_members()
    return result

for dirname,dirnames,filenames in os.walk(src):
    for filename in filenames:
        if filename.endswith('.tmpl'):
            src_path = os.path.join(dirname, filename)
            dst_path = os.path.join(dst, src_path[len(src)+1:][:-4]+'rst')
            content = process_file(src_path)

            # Skip if already up to date
            if os.path.exists(dst_path):
                try:
                    old_content = open(dst_path, 'r').read()
                    if content == old_content:
                        continue
                except IOError:
                    pass

            # Update
            print src_path+' -> '+dst_path
            if not os.path.exists(os.path.dirname(dst_path)):
                os.makedirs(os.path.dirname(dst_path))
            with open(dst_path, 'w') as f:
                f.write(content)
