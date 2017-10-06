author = 'djungelorm'
version = '0.3.11'
ksp_version = '1.3.1.1891'
ksp_version_parts = ksp_version.split('.')

def get_version_parts(version):
    version_parts = version.partition('-')[0].split('.')
    if '-' in version:
        version_parts.extend(version.partition('-')[2].split('-'))
    return version_parts

version_parts = get_version_parts(version)

# Python version x.y.z[+w.w]
python_version = \
    '.'.join(version_parts) if len(version_parts) == 3 \
    else '.'.join(version_parts[:3])+'+'+'.'.join(version_parts[3:])

# C# assembly version: x.y.z[.w]
assembly_version = '.'.join(version_parts[:4])

# Nuget package version: x.y.z[-buildw]
nuget_version = \
    '.'.join(version_parts[:3]) if len(version_parts) == 3 \
    else '.'.join(version_parts[:3])+'-build'+version_parts[3]

# Lua rock version: x.y.z[.w]
lua_version = '.'.join(version_parts[:4])

# KSP-AVC versions
avc_version = \
    '"MAJOR": %s, "MINOR": %s, "PATCH": %s' % (version_parts[0],version_parts[1],version_parts[2]) if len(version_parts) == 3 \
    else '"MAJOR": %s, "MINOR": %s, "PATCH": %s, "BUILD": %s' % (version_parts[0],version_parts[1],version_parts[2],version_parts[3])
ksp_avc_version = '"MAJOR": %s, "MINOR": %s, "PATCH": %s' % (ksp_version_parts[0],ksp_version_parts[1],ksp_version_parts[2])
