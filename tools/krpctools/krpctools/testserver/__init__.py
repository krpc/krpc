"""Runs the TestServer bundled with krpctools. Every argument is passed to the server."""

import os
import subprocess
import sys

from ..dotnet import command, testserver

if sys.platform == "win32":
    import ctypes
    from ctypes import wintypes

    # From winnt.h
    JOB_OBJECT_EXTENDED_LIMIT_INFORMATION = 9
    JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x2000

    class _BasicLimitInformation(ctypes.Structure):
        _fields_ = [  # noqa: RUF012
            ("PerProcessUserTimeLimit", ctypes.c_int64),
            ("PerJobUserTimeLimit", ctypes.c_int64),
            ("LimitFlags", wintypes.DWORD),
            ("MinimumWorkingSetSize", ctypes.c_size_t),
            ("MaximumWorkingSetSize", ctypes.c_size_t),
            ("ActiveProcessLimit", wintypes.DWORD),
            ("Affinity", ctypes.c_size_t),
            ("PriorityClass", wintypes.DWORD),
            ("SchedulingClass", wintypes.DWORD),
        ]

    class _IoCounters(ctypes.Structure):
        _fields_ = [  # noqa: RUF012
            (name, ctypes.c_uint64)
            for name in (
                "ReadOperationCount",
                "WriteOperationCount",
                "OtherOperationCount",
                "ReadTransferCount",
                "WriteTransferCount",
                "OtherTransferCount",
            )
        ]

    class _ExtendedLimitInformation(ctypes.Structure):
        _fields_ = [  # noqa: RUF012
            ("BasicLimitInformation", _BasicLimitInformation),
            ("IoInfo", _IoCounters),
            ("ProcessMemoryLimit", ctypes.c_size_t),
            ("JobMemoryLimit", ctypes.c_size_t),
            ("PeakProcessMemoryUsed", ctypes.c_size_t),
            ("PeakJobMemoryUsed", ctypes.c_size_t),
        ]

    def _kill_on_exit(process):
        """Put a child process in a job that Windows kills when this process exits, however
        it exits. The job handle is left open, and Windows closes it with this process."""
        kernel32 = ctypes.WinDLL("kernel32", use_last_error=True)
        kernel32.CreateJobObjectW.restype = wintypes.HANDLE
        kernel32.CreateJobObjectW.argtypes = [ctypes.c_void_p, wintypes.LPCWSTR]
        kernel32.SetInformationJobObject.restype = wintypes.BOOL
        kernel32.SetInformationJobObject.argtypes = [
            wintypes.HANDLE,
            ctypes.c_int,
            ctypes.c_void_p,
            wintypes.DWORD,
        ]
        kernel32.AssignProcessToJobObject.restype = wintypes.BOOL
        kernel32.AssignProcessToJobObject.argtypes = [wintypes.HANDLE, wintypes.HANDLE]

        job = kernel32.CreateJobObjectW(None, None)
        if not job:
            raise ctypes.WinError(ctypes.get_last_error())
        info = _ExtendedLimitInformation()
        info.BasicLimitInformation.LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
        if not kernel32.SetInformationJobObject(
            job,
            JOB_OBJECT_EXTENDED_LIMIT_INFORMATION,
            ctypes.byref(info),
            ctypes.sizeof(info),
        ):
            raise ctypes.WinError(ctypes.get_last_error())
        handle = process._handle  # noqa: SLF001
        if not kernel32.AssignProcessToJobObject(job, handle):
            raise ctypes.WinError(ctypes.get_last_error())


def main():
    try:
        args = command(testserver(), sys.argv[1:])
    except RuntimeError as ex:
        sys.stderr.write("Error: %s\n" % str(ex))
        return 1
    if sys.platform != "win32":
        # Replace this process, so signals reach the server
        os.execv(args[0], args)
    # Windows has no exec, so the server is a child process. It is killed with this one, so
    # stopping krpc-testserver stops the server.
    process = subprocess.Popen(args)
    try:
        _kill_on_exit(process)
    except OSError:
        # A server that has already exited, such as for --help, cannot join the job
        if process.poll() is None:
            process.kill()
            raise
    while True:
        try:
            return process.wait()
        except KeyboardInterrupt:
            # The server receives the Ctrl+C too, and exits with its own code
            pass


if __name__ == "__main__":
    sys.exit(main())
