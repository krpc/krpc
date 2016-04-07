author = 'djungelorm'
#FIXME: need to set the version using git describe, but not currently possible using Bazel
version = '0.2.2'
ksp_version = '1.0.5'
ksp_version_parts = ksp_version.split('.')

version_parts = version.split('-')[0].split('.')
if '-' in version:
   version_parts.append(version.split('-')[1])

# C# assembly version: x.y.z[.w]
assembly_version = '.'.join(version_parts)

# Nuget package version: x.y.z[-buildw]
if len(version_parts) == 3:
   #TODO: remove pre suffix when Google.Protobuf is released
   nuget_version = '.'.join(version_parts[:3])+'-pre'
else:
   nuget_version = '.'.join(version_parts[:3])+'-build'+version_parts[3]

# Lua rock version: x.y.z[.w]-0
lua_version = '.'.join(version_parts)+'-0'

# KSP-AVC versions
if len(version_parts) == 3:
   avc_version = '"MAJOR": %s, "MINOR": %s, "PATCH": %s' % (version_parts[0],version_parts[1],version_parts[2])
else:
   avc_version = '"MAJOR": %s, "MINOR": %s, "PATCH": %s, "BUILD": %s' % (version_parts[0],version_parts[1],version_parts[2],version_parts[3])
ksp_avc_version = '"MAJOR": %s, "MINOR": %s, "PATCH": %s' % (ksp_version_parts[0],ksp_version_parts[1],ksp_version_parts[2])
