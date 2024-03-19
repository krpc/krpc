# Start Building (en-US version)

NOTE: The file is built under Windows 10 22H2 (19045.3930) in zh-CN and Visual Studio 2022 17.5.5 in zh-CN. If there's something in Chinese, please refer to zh-CN version or use Google Translation.

### Installing vcpkg

1. Use `git clone "https://github.com/Microsoft/vcpkg.git"` in a proper directory to clone the repository. This will create an `vcpkg` folder under target directory.
2. Add Environmental Variables：
    - Add into Path. e.g.`D:\vcpkg`
3. Open `vcpkg` folder, and run `bootstrap-vcpkg.bat`. The output will be:

> D:\vcpkg>bootstrap-vcpkg.bat<br />
> Downloading https://github.com/microsoft/vcpkg-tool/releases/download/2024-02-07/vcpkg.exe -> D:\vcpkg\vcpkg.exe (using IE proxy: 127.0.0.1:5230)... done.<br />
> Validating signature... done.<br />
> <br />
> vcpkg package management program version 2024-02-07-8a83681f921b10d86ae626fd833c253f4f8c355b<br />
> <br />
> See LICENSE.txt for license information.<br />
> Telemetry<br />
> ---------<br />
> vcpkg collects usage data in order to help us improve your experience.<br />
> The data collected by Microsoft is anonymous.<br />
> You can opt-out of telemetry by re-running the bootstrap-vcpkg script with -disableMetrics,<br />
> passing --disable-metrics to vcpkg on the command line,<br />
> or by setting the VCPKG_DISABLE_METRICS environment variable.<br />
> <br />
> Read more about vcpkg telemetry at docs/about/privacy.md<br />
> <br />
> D:\vcpkg>

&emsp;&emsp;&emsp;Or see an newly-creates `vcpkg.exe` under root directory。

4. Open Powershell `vcpkg` directory in Powershell and input `vcpkg integrate install` to integrate vcpkg into Visual Studio. The output will be:

> PS D:\vcpkg> vcpkg integrate install<br />
> Applied user-wide integration for this vcpkg root.<br />
> CMake projects should use: "-DCMAKE_TOOLCHAIN_FILE=D:/vcpkg/scripts/buildsystems/vcpkg.cmake"<br />
> <br />
> All MSBuild C++ projects can now #include any installed libraries. Linking will be handled automatically. Installing new libraries will make them instantly available.<br />
> PS D:\vcpkg> 

### Install protobuf
5. Input `vcpkg install protobuf protobuf:x64-windows` in Powershell. The output will be:
> PS D:\vcpkg> vcpkg install protobuf protobuf:x64-windows<br />
> warning: In the September 2023 release, the default triplet for vcpkg libraries changed from x86-windows to the detected host triplet (x64-windows). For the old behavior, add --triplet x86-windows . To suppress this message, add --triplet x64-windows .<br />
> Computing installation plan...<br />
> The following packages will be built and installed:<br />
>     protobuf:x64-windows@3.21.12#1<br />
> Detecting compiler hash for triplet x64-windows...<br />
> -- Automatically setting %HTTP(S)_PROXY% environment variables to "127.0.0.1:5230".<br />
> Restored 0 package(s) from C:\Users\Peter\AppData\Local\vcpkg\archives in 179 us. Use --debug to see more details.<br />
> Installing 1/1 protobuf:x64-windows@3.21.12#1...<br />
> Building protobuf:x64-windows@3.21.12#1...<br />
> -- Using cached protocolbuffers-protobuf-v3.21.12.tar.gz.<br />
> -- Cleaning sources at D:/vcpkg/buildtrees/protobuf/src/v3.21.12-fdb7676342.clean. Use --editable to skip cleaning for the packages you specify.<br />
> -- Extracting source D:/vcpkg/downloads/protocolbuffers-protobuf-v3.21.12.tar.gz<br />
> -- Applying patch fix-static-build.patch<br />
> -- Applying patch fix-default-proto-file-path.patch<br />
> -- Applying patch compile_options.patch<br />
> -- Using source at D:/vcpkg/buildtrees/protobuf/src/v3.21.12-fdb7676342.clean<br />
> -- Found external ninja('1.11.0').<br />
> -- Configuring x64-windows<br />
> -- Building x64-windows-dbg<br />
> -- Building x64-windows-rel<br />
> CMake Warning at scripts/cmake/vcpkg_copy_pdbs.cmake:44 (message):<br />
>   Could not find a matching pdb file for:<br />
> <br />
>       D:/vcpkg/packages/protobuf_x64-windows/bin/libprotobuf-lite.dll<br />
>       D:/vcpkg/packages/protobuf_x64-windows/bin/libprotobuf.dll<br />
>       D:/vcpkg/packages/protobuf_x64-windows/bin/libprotoc.dll<br />
>       D:/vcpkg/packages/protobuf_x64-windows/debug/bin/libprotobuf-lited.dll<br />
>       D:/vcpkg/packages/protobuf_x64-windows/debug/bin/libprotobufd.dll<br />
>       D:/vcpkg/packages/protobuf_x64-windows/debug/bin/libprotocd.dll<br />
> <br />
> Call Stack (most recent call first):<br />
>   ports/protobuf/portfile.cmake:123 (vcpkg_copy_pdbs)<br />
>   scripts/ports.cmake:170 (include)<br />
> <br />
> <br />
> -- Fixing pkgconfig file: D:/vcpkg/packages/protobuf_x64-windows/lib/pkgconfig/protobuf-lite.pc<br />
> -- Fixing pkgconfig file: D:/vcpkg/packages/protobuf_x64-windows/lib/pkgconfig/protobuf.pc<br />
> -- Using cached msys2-mingw-w64-x86_64-pkgconf-1~2.1.0-1-any.pkg.tar.zst.<br />
> -- Using cached msys2-msys2-runtime-3.4.10-4-x86_64.pkg.tar.zst.<br />
> -- Using msys root at D:/vcpkg/downloads/tools/msys2/fdbea3694fb5c0d4<br />
> -- Fixing pkgconfig file: D:/vcpkg/packages/protobuf_x64-windows/debug/lib/pkgconfig/protobuf-lite.pc<br />
> -- Fixing pkgconfig file: D:/vcpkg/packages/protobuf_x64-windows/debug/lib/pkgconfig/protobuf.pc<br />
> -- Installing: D:/vcpkg/packages/protobuf_x64-windows/share/protobuf/copyright<br />
> -- Performing post-build validation<br />
> Stored binaries in 1 destinations in 1.4 s.<br />
> Elapsed time to handle protobuf:x64-windows: 6.1 min<br />
> protobuf:x64-windows package ABI: c30ce9286ddf61354781f5759c28b705188dcc28ae0aa6d1d09771e8869a6ae0<br />
> Total install time: 6.1 min<br />
> protobuf provides CMake targets:<br />
> <br />
>   \# this is heuristically generated, and may not be correct<br />
>   find_package(protobuf CONFIG REQUIRED)<br />
>   target_link_libraries(main PRIVATE protobuf::libprotoc protobuf::libprotobuf protobuf::libprotobuf-lite)<br />
> <br />
> protobuf provides pkg-config modules:<br />
> <br />
>     # Google's Data Interchange Format<br />
>     protobuf-lite<br />
> <br />
>     # Google's Data Interchange Format<br />
>     protobuf<br />
> <br />
> PS D:\vcpkg>

### Install asio

6. Download asio from [https://think-async.com/Asio/Download.html](https://think-async.com/Asio/Download.html "Offical Website") and unzip into a proper directory.

### Install cmake

7. Download cmake from [https://cmake.org/download/](https://cmake.org/download/ "Official Website") and install it.<br />
**⚠️Note to choose`◉ Add CMake to the system PATH for all users` instead of `⌾ Do not add CMake to the system PATH`**

### Compile kRPC

8. Download neweat kRPC client library from [https://github.com/krpc/krpc/releases](https://github.com/krpc/krpc/releases "Github Repository") and unzip to a proper directory.
9. Open `./CMakeLists.txt`, then add the directory for asio and protobuf into the previous or next line of `project(kRPC)`. The file could be:
```
...

set(ASIO_INCLUDE_DIR D:/asio-1.28.0/include)
list(APPEND CMAKE_PREFIX_PATH "D:/vcpkg/packages/protobuf_x64-windows")
project(kRPC)

...
```
10. _(Optional)_ Change build typr from Debug to Release. To make it, simply change `set(CMAKE_BUILD_TYPE "Debug")` to `set(CMAKE_BUILD_TYPE "Release")` in `./CMakeLists.txt`.

11. Open Powershell under the directory of krpc and run `cmake .`. The output will be this below, and you'll see `kRPC.sln` file.
> PS D:\Projects\krpc-cpp-0.5.2>  cmake .<br />
> -- Building for: Visual Studio 17 2022<br />
> CMake Deprecation Warning at CMakeLists.txt:1 (cmake_minimum_required):<br />
>   Compatibility with CMake < 3.5 will be removed from a future version of<br />
>   CMake.<br />
> <br />
>   Update the VERSION argument <min> value or use a ...<max> suffix to tell<br />
>   CMake that the project does not need compatibility with older versions.<br />
> <br />
> <br />
> -- Selecting Windows SDK version 10.0.22000.0 to target Windows 10.0.19045.<br />
> -- The C compiler identification is MSVC 19.35.32217.1<br />
> -- The CXX compiler identification is MSVC 19.35.32217.1<br />
> -- Detecting C compiler ABI info<br />
> -- Detecting C compiler ABI info - done<br />
> -- Check for working C compiler: D:/Microsoft Visual Studio/2022/VC/Tools/MSVC/14.35.32215/bin/Hostx64/x64/cl.exe - skipped<br />
> -- Detecting C compile features<br />
> -- Detecting C compile features - done<br />
> -- Detecting CXX compiler ABI info<br />
> -- Detecting CXX compiler ABI info - done<br />
> -- Check for working CXX compiler: D:/Microsoft Visual Studio/2022/VC/Tools/MSVC/14.35.32215/bin/Hostx64/x64/cl.exe - skipped<br />
> -- Detecting CXX compile features<br />
> -- Detecting CXX compile features - done<br />
> -- Found Protobuf: optimized;D:/vcpkg/installed/x64-windows/lib/libprotobuf.lib;debug;D:/vcpkg/installed/x64-windows/debug/lib/libprotobufd.lib (found suitable version "3.21.12", minimum required is "3.2")<br />
> -- Found Protobuf compiler D:/vcpkg/installed/x64-windows/tools/protobuf/protoc.exe<br />
> -- Configuring done (20.6s)<br />
> -- Generating done (0.1s)<br />
> -- Build files have been written to: D:/Projects/krpc-cpp-0.5.2<br />
> PS D:\Projects\krpc-cpp-0.5.2> <br />

12. Open `kRPC.sln` and generate using "Local Windows Debugger".<br />
Hang on a minute and you'll see this in the "Output" panel:
```
...

========== Build: 3 succeeded, 0 failed, 0 up-to-date, 0 skipped ==========

...
```
&emsp;&emsp;&emsp;If "succeeded" is "3", you can dismiss the notification of "Access Denied".

Building finishes here. Generated file will be in folder `./Release` or `./Debug` under root directory of krpc.