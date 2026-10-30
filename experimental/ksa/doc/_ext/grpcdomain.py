"""A Sphinx domain for gRPC services, with signatures in the form of a .proto file.

    .. grpc:package:: krpc.ksa

    .. grpc:service:: VehicleService

       .. grpc:rpc:: GetName (Vehicle.GetNameRequest) returns (Vehicle.GetNameResponse)

    .. grpc:enum:: Situation

       .. grpc:value:: SITUATION_LANDED

A message holds fields and an error is a named error, as a service holds its methods. Each
object is named by its package and parents, as krpc.ksa.VehicleService.GetName, and the role
of each object type, such as :grpc:rpc:, resolves a name relative to the current package and
parent.
"""

import re

from docutils import nodes
from sphinx import addnodes
from sphinx.directives import ObjectDescription
from sphinx.domains import Domain, ObjType
from sphinx.roles import XRefRole
from sphinx.util.docutils import SphinxDirective
from sphinx.util.nodes import make_refnode


class GrpcPackage(SphinxDirective):
    """Set the package of the objects that follow in the document."""

    required_arguments = 1

    def run(self):
        self.env.ref_context["grpc:package"] = self.arguments[0]
        return []


class GrpcObject(ObjectDescription):
    # Whether the object holds other objects
    nested = False

    def handle_signature(self, sig, signode):
        name, _, rest = sig.strip().partition(" ")
        signode += addnodes.desc_sig_keyword(self.objtype, self.objtype)
        signode += addnodes.desc_sig_space()
        signode += addnodes.desc_name(name, name)
        # The request and response of an rpc
        if rest:
            signode += addnodes.desc_sig_space()
        for token in re.findall(r"[()]|\w[\w.]*", rest):
            if token in "()":
                signode += addnodes.desc_sig_punctuation(token, token)
            elif token == "returns":
                signode += addnodes.desc_sig_space()
                signode += addnodes.desc_sig_keyword(token, token)
                signode += addnodes.desc_sig_space()
            else:
                signode += addnodes.desc_sig_name(token, token)
        parents = self.env.ref_context.get("grpc:parents")
        if parents:
            prefix = parents[-1]
        else:
            prefix = self.env.ref_context.get("grpc:package")
        return f"{prefix}.{name}" if prefix else name

    def add_target_and_index(self, name, sig, signode):
        signode["ids"].append(name)
        self.env.domains["grpc"].note_object(name, self.objtype, self.get_location())
        short = name.rsplit(".", 1)[-1]
        self.indexnode["entries"].append(
            ("single", f"{short} ({self.objtype})", name, "", None)
        )

    def before_content(self):
        if self.nested and self.names:
            self.env.ref_context.setdefault("grpc:parents", []).append(self.names[-1])

    def after_content(self):
        if self.nested and self.names:
            self.env.ref_context["grpc:parents"].pop()

    def _toc_entry_name(self, sig_node):
        return sig_node["ids"][0].rsplit(".", 1)[-1] if sig_node["ids"] else ""


class GrpcContainer(GrpcObject):
    nested = True


class GrpcXRefRole(XRefRole):
    def process_link(self, env, refnode, has_explicit_title, title, target):
        refnode["grpc:package"] = env.ref_context.get("grpc:package")
        parents = env.ref_context.get("grpc:parents")
        refnode["grpc:parent"] = parents[-1] if parents else None
        return title, target


class GrpcDomain(Domain):
    name = "grpc"
    label = "gRPC"
    object_types = {
        "service": ObjType("service", "service"),
        "rpc": ObjType("rpc", "rpc"),
        "message": ObjType("message", "message"),
        "field": ObjType("field", "field"),
        "enum": ObjType("enum", "enum"),
        "value": ObjType("value", "value"),
        "error": ObjType("error", "error"),
    }
    directives = {
        "package": GrpcPackage,
        "service": GrpcContainer,
        "rpc": GrpcObject,
        "message": GrpcContainer,
        "field": GrpcObject,
        "enum": GrpcContainer,
        "value": GrpcObject,
        "error": GrpcObject,
    }
    roles = {name: GrpcXRefRole(innernodeclass=nodes.literal) for name in object_types}
    initial_data = {"objects": {}}

    @property
    def objects(self):
        # Full name to the document and object type
        return self.data.setdefault("objects", {})

    def note_object(self, name, objtype, location=None):
        self.objects[name] = (self.env.docname, objtype)

    def clear_doc(self, docname):
        for name, (doc, _) in list(self.objects.items()):
            if doc == docname:
                del self.objects[name]

    def merge_domaindata(self, docnames, otherdata):
        for name, (doc, objtype) in otherdata["objects"].items():
            if doc in docnames:
                self.objects[name] = (doc, objtype)

    def find(self, node, target):
        candidates = [
            f"{node.get('grpc:parent')}.{target}",
            f"{node.get('grpc:package')}.{target}",
            target,
        ]
        for name in candidates:
            if name in self.objects:
                return name
        return None

    def resolve_xref(self, env, fromdocname, builder, typ, target, node, contnode):
        name = self.find(node, target)
        if name is None or self.objects[name][1] not in self.objtypes_for_role(typ):
            return None
        return make_refnode(
            builder, fromdocname, self.objects[name][0], name, contnode, name
        )

    def resolve_any_xref(self, env, fromdocname, builder, target, node, contnode):
        name = self.find(node, target)
        if name is None:
            return []
        docname, objtype = self.objects[name]
        return [
            (
                f"grpc:{self.role_for_objtype(objtype)}",
                make_refnode(builder, fromdocname, docname, name, contnode, name),
            )
        ]

    def get_objects(self):
        for name, (docname, objtype) in self.objects.items():
            yield name, name, objtype, docname, name, 1


def setup(app):
    app.add_domain(GrpcDomain)
    return {"version": "1.0", "parallel_read_safe": True, "parallel_write_safe": True}
