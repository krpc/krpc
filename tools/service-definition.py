import argparse
import krpc
import yaml

parser = argparse.ArgumentParser(description='Outputs a YAML summary file for a given service or all services')
parser.add_argument('--address', dest='address', default='127.0.0.1', action='store', help='Server address (default: 127.0.0.1)')
parser.add_argument('--rpc-port', dest='rpc_port', default=50000, type=int, action='store', help='RPC port (default: 50000)')
parser.add_argument('service', nargs='?', action='store', help='Service name (default: all services)')
args = parser.parse_args()

conn = krpc.connect(address=args.address, rpc_port=args.rpc_port, stream_port=None)

data = {}
services = conn.krpc.get_services().services
for service in services:
    if args.service is not None and service.name != args.service:
        continue
    service_data = {}
    for procedure in service.procedures:
        procedure_data = {}
        if len(procedure.parameters) > 0:
            parameters_data = {}
            for i,parameter in enumerate(procedure.parameters):
                parameters_data[str(parameter.name)] = {
                    'position': i,
                    'type': str(parameter.type)
                }
            procedure_data['parameters'] = parameters_data

        if len(procedure.attributes) > 0:
            procedure_data['attributes'] = [str(a) for a in procedure.attributes]
        service_data[str(procedure.name)] = procedure_data
    data[str(service.name)] = service_data

print yaml.dump(data, default_flow_style=False)
