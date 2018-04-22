// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TraceCollector
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio.TestTools.Diagnostics;

    /// <summary>
    /// Helper class used to add a child process to a job object so that it terminates when
    /// the parent process dies
    /// </summary>
    /// <summary>An interface to the Windows Job Objects API.</summary>
    internal class ProcessJobObject : IDisposable
    {
        /// <summary>
        /// Creates a job object
        /// </summary>
        /// <returns>Handle to the job object created</returns>
        public ProcessJobObject()
        {
            CreateJobObject();
        }

        /// <summary>
        /// Helper function to add a process to the job object
        /// </summary>
        /// <param name="handle">Handle of the process to be added</param>
        public void AddProcess(IntPtr handle)
        {
            if (this.jobHandle != IntPtr.Zero)
            {
                if (!WinAPI.AssignProcessToJobObject(this.jobHandle, handle))
                {
                    EqtTrace.Warning("AddProcess : Failed to AddProcess {0}", Marshal.GetLastWin32Error());
                }
            }
            else
            {
                EqtTrace.Warning("AddProcess : Ignoring as job object is not created");
            }
        }

        #region Private Members

        /// <summary>
        /// Helper function to create job object
        /// </summary>
        private void CreateJobObject()
        {
            this.jobHandle = WinAPI.CreateJobObject(IntPtr.Zero, null);
            if (this.jobHandle == IntPtr.Zero)
            {
                EqtTrace.Warning("CreateJobObject : Failed {0}", Marshal.GetLastWin32Error());
            }

            if (ProcessJobObject.Is32Bit)
            {
                BasicLimits32 basicInfo = new BasicLimits32();
                basicInfo.LimitFlags = LimitFlags.LimitKillOnJobClose;

                ExtendedLimits32 extendedInfo = new ExtendedLimits32();
                extendedInfo.BasicLimits = basicInfo;

                JobObjectInfo info = new JobObjectInfo();
                info.basicLimits32 = basicInfo;
                info.extendedLimits32 = extendedInfo;

                if (!WinAPI.SetInformationJobObject(jobHandle, JobObjectInfoClass.ExtendedLimitInformation, ref info,
                    Marshal.SizeOf <ExtendedLimits32>()))
                {
                    EqtTrace.Warning("CreateJobObject [32] : Failed to setInformation {0}", Marshal.GetLastWin32Error());
                }
            }
            else
            {
                BasicLimits64 basicInfo = new BasicLimits64();
                basicInfo.LimitFlags = LimitFlags.LimitKillOnJobClose;

                ExtendedLimits64 extendedInfo = new ExtendedLimits64();
                extendedInfo.BasicLimits = basicInfo;

                JobObjectInfo info = new JobObjectInfo();
                info.basicLimits64 = basicInfo;
                info.extendedLimits64 = extendedInfo;

                if (!WinAPI.SetInformationJobObject(jobHandle, JobObjectInfoClass.ExtendedLimitInformation, ref info,
                    Marshal.SizeOf<ExtendedLimits64>()))
                {
                    EqtTrace.Warning("CreateJobObject [64] : Failed to setInformation {0}", Marshal.GetLastWin32Error());
                }
            }
        }

        /// <summary>
        /// Job handle created by the CreateJobObject
        /// </summary>
        private IntPtr jobHandle;

        /// <summary>
        /// Set to true when disposed
        /// </summary>
        private volatile bool disposed;

        #endregion

        #region Native 32/64 Bit Switching Flag

        /// <summary>
        /// The structures returned by Windows are different sizes depending on whether
        /// the operating system is running in 32bit or 64bit mode.
        /// </summary>
        private static readonly bool Is32Bit = (IntPtr.Size == 4);

        #endregion

        #region JobObjectInfoClass Enumeration

        /// <summary>
        /// Information class for the limits to be set. This parameter can be one of
        /// the following values.
        /// </summary>
        private enum JobObjectInfoClass
        {
            /// <summary>
            /// The lpJobObjectInfo parameter is a pointer to a
            /// JOBOBJECT_BASIC_ACCOUNTING_INFORMATION structure.
            /// </summary>
            BasicAccountingInformation = 1,

            /// <summary>
            /// The lpJobObjectInfo parameter is a pointer to a
            /// JOBOBJECT_BASIC_LIMIT_INFORMATION structure.
            /// </summary>
            BasicLimitInformation = 2,

            /// <summary>
            /// The lpJobObjectInfo parameter is a pointer to a
            /// JOBOBJECT_BASIC_PROCESS_ID_LIST structure.
            /// </summary>
            BasicProcessIdList = 3,

            /// <summary>
            /// The lpJobObjectInfo parameter is a pointer to a
            /// JOBOBJECT_BASIC_UI_RESTRICTIONS structure.
            /// </summary>
            BasicUIRestrictions = 4,

            /// <summary>
            /// The lpJobObjectInfo parameter is a pointer to a
            /// JOBOBJECT_SECURITY_LIMIT_INFORMATION structure.
            /// The hJob handle must have the JOB_OBJECT_SET_SECURITY_ATTRIBUTES
            /// access right associated with it.
            /// </summary>
            SecurityLimitInformation = 5,

            /// <summary>
            /// The lpJobObjectInfo parameter is a pointer to a
            /// JOBOBJECT_END_OF_JOB_TIME_INFORMATION structure.
            /// </summary>
            EndOfJobTimeInformation = 6,

            /// <summary>
            /// The lpJobObjectInfo parameter is a pointer to a
            /// JOBOBJECT_ASSOCIATE_COMPLETION_PORT structure.
            /// </summary>
            AssociateCompletionPortInformation = 7,

            /// <summary>
            /// The lpJobObjectInfo parameter is a pointer to a
            /// JOBOBJECT_BASIC_AND_IO_ACCOUNTING_INFORMATION structure.
            /// </summary>
            BasicAndIoAccountingInformation = 8,

            /// <summary>
            /// The lpJobObjectInfo parameter is a pointer to a
            /// JOBOBJECT_EXTENDED_LIMIT_INFORMATION structure.
            /// </summary>
            ExtendedLimitInformation = 9
        }

        #endregion

        #region LimitFlags Enumeration

        /// <summary>
        /// Limit flags that are in effect. This member is a bit field that determines
        /// whether other structure members are used. Any combination of the following
        /// values can be specified.
        /// </summary>
        [Flags]
        private enum LimitFlags
        {
            /// <summary>
            /// Causes all processes associated with the job to use the same minimum and maximum working set sizes.
            /// </summary>
            LimitWorkingSet = 0x00000001,

            /// <summary>
            /// Establishes a user-mode execution time limit for each currently active process
            /// and for all future processes associated with the job.
            /// </summary>
            LimitProcessTime = 0x00000002,

            /// <summary>
            /// Establishes a user-mode execution time limit for the job. This flag cannot
            /// be used with JOB_OBJECT_LIMIT_PRESERVE_JOB_TIME.
            /// </summary>
            LimitJobTime = 0x00000004,

            /// <summary>
            /// Establishes a maximum number of simultaneously active processes associated
            /// with the job.
            /// </summary>
            LimitActiveProcesses = 0x00000008,

            /// <summary>
            /// Causes all processes associated with the job to use the same processor
            /// affinity.
            /// </summary>
            LimitAffinity = 0x00000010,

            /// <summary>
            /// Causes all processes associated with the job to use the same priority class.
            /// For more information, see Scheduling Priorities.
            /// </summary>
            LimitPriorityClass = 0x00000020,

            /// <summary>
            /// Preserves any job time limits you previously set. As long as this flag is
            /// set, you can establish a per-job time limit once, then alter other limits
            /// in subsequent calls. This flag cannot be used with JOB_OBJECT_LIMIT_JOB_TIME.
            /// </summary>
            PreserveJobTime = 0x00000040,

            /// <summary>
            /// Causes all processes in the job to use the same scheduling class.
            /// </summary>
            LimitSchedulingClass = 0x00000080,

            /// <summary>
            /// Causes all processes associated with the job to limit their committed memory.
            /// When a process attempts to commit memory that would exceed the per-process
            /// limit, it fails. If the job object is associated with a completion port, a
            /// JOB_OBJECT_MSG_PROCESS_MEMORY_LIMIT message is sent to the completion port.
            /// This limit requires use of a JOBOBJECT_EXTENDED_LIMIT_INFORMATION structure.
            /// Its BasicLimitInformation member is a JOBOBJECT_BASIC_LIMIT_INFORMATION
            /// structure.
            /// </summary>
            LimitProcessMemory = 0x00000100,

            /// <summary>
            /// Causes all processes associated with the job to limit the job-wide sum of
            /// their committed memory. When a process attempts to commit memory that would
            /// exceed the job-wide limit, it fails. If the job object is associated with a
            /// completion port, a JOB_OBJECT_MSG_JOB_MEMORY_LIMIT message is sent to the
            /// completion port.  This limit requires use of a
            /// JOBOBJECT_EXTENDED_LIMIT_INFORMATION structure. Its BasicLimitInformation
            /// member is a JOBOBJECT_BASIC_LIMIT_INFORMATION structure.
            /// </summary>
            LimitJobMemory = 0x00000200,

            /// <summary>
            /// Forces a call to the SetErrorMode function with the SEM_NOGPFAULTERRORBOX
            /// flag for each process associated with the job.  If an exception occurs and
            /// the system calls the UnhandledExceptionFilter function, the debugger will
            /// be given a chance to act. If there is no debugger, the functions returns
            /// EXCEPTION_EXECUTE_HANDLER. Normally, this will cause termination of the
            /// process with the exception code as the exit status.  This limit requires
            /// use of a JOBOBJECT_EXTENDED_LIMIT_INFORMATION structure. Its
            /// BasicLimitInformation member is a JOBOBJECT_BASIC_LIMIT_INFORMATION structure.
            /// </summary>
            DieOnUnhandledException = 0x00000400,

            /// <summary>
            /// If any process associated with the job creates a child process using the
            /// CREATE_BREAKAWAY_FROM_JOB flag while this limit is in effect, the child
            /// process is not associated with the job.  This limit requires use of a
            /// JOBOBJECT_EXTENDED_LIMIT_INFORMATION structure. Its BasicLimitInformation
            /// member is a JOBOBJECT_BASIC_LIMIT_INFORMATION structure.
            /// </summary>
            LimitBreakawayOk = 0x00000800,

            /// <summary>
            /// Allows any process associated with the job to create child processes
            /// that are not associated with the job.  This limit requires use of a
            /// JOBOBJECT_EXTENDED_LIMIT_INFORMATION structure. Its BasicLimitInformation
            /// member is a JOBOBJECT_BASIC_LIMIT_INFORMATION structure.
            /// </summary>
            LimitSilentBreakawayOk = 0x00001000,

            /// <summary>
            /// Causes all processes associated with the job to terminate when the last
            /// handle to the job is closed.  This limit requires use of a
            /// JOBOBJECT_EXTENDED_LIMIT_INFORMATION structure. Its BasicLimitInformation
            /// member is a JOBOBJECT_BASIC_LIMIT_INFORMATION structure.
            /// Windows 2000:  This flag is not supported.
            /// </summary>
            LimitKillOnJobClose = 0x00002000
        }

        #endregion

        #region IoCounters Structures

        /// <summary>
        /// Various counters for different types of IO operations
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        private struct IoCounters32
        {
            /// <summary>
            /// The number of read operations.
            /// </summary>
            [FieldOffset(0)]
            public ulong ReadOperationCount;

            /// <summary>
            /// The number of write operations.
            /// </summary>
            [FieldOffset(8)]
            public ulong WriteOperationCount;

            /// <summary>
            /// The number of other operations.
            /// </summary>
            [FieldOffset(16)]
            public ulong OtherOperationCount;

            /// <summary>
            /// The number of read transfers.
            /// </summary>
            [FieldOffset(24)]
            public ulong ReadTransferCount;

            /// <summary>
            /// The number of write transfers.
            /// </summary>
            [FieldOffset(32)]
            public ulong WriteTransferCount;

            /// <summary>
            /// The number of other transfers.
            /// </summary>
            [FieldOffset(40)]
            public ulong OtherTransferCount;
        }

        /// <summary>
        /// Various counters for different types of IO operations. 
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        private struct IoCounters64
        {
            /// <summary>
            /// The number of read operations.
            /// </summary>
            [FieldOffset(0)]
            public ulong ReadOperationCount;

            /// <summary>
            /// The number of write operations.
            /// </summary>
            [FieldOffset(8)]
            public ulong WriteOperationCount;

            /// <summary>
            /// The number of other operations.
            /// </summary>
            [FieldOffset(16)]
            public ulong OtherOperationCount;

            /// <summary>
            /// The number of read transfers.
            /// </summary>
            [FieldOffset(24)]
            public ulong ReadTransferCount;

            /// <summary>
            /// The number of write transfers.
            /// </summary>
            [FieldOffset(32)]
            public ulong WriteTransferCount;

            /// <summary>
            /// The number of other transfers.
            /// </summary>
            [FieldOffset(40)]
            public ulong OtherTransferCount;
        }

        #endregion

        #region BasicLimits Structures

        /// <summary>
        /// The JOBOBJECT_BASIC_LIMIT_INFORMATION structure contains basic limit
        /// information for a job object.
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        private struct BasicLimits32
        {
            /// <summary>
            /// If LimitFlags specifies JOB_OBJECT_LIMIT_PROCESS_TIME, this member is
            /// the per-process user-mode execution time limit, in 100-nanosecond ticks.
            /// Otherwise, this member is ignored.  The system periodically checks to
            /// determine whether each process associated with the job has accumulated
            /// more user-mode time than the set limit. If it has, the process is terminated.
            /// </summary>
            [FieldOffset(0)]
            public long PerProcessUserTimeLimit;

            /// <summary>
            /// If LimitFlags specifies JOB_OBJECT_LIMIT_JOB_TIME, this member is the
            /// per-job user-mode execution time limit, in 100-nanosecond ticks. Otherwise,
            /// this member is ignored. The system adds the current time of the processes
            /// associated with the job to this limit. For example, if you set this limit
            /// to 1 minute, and the job has a process that has accumulated 5 minutes of
            /// user-mode time, the limit actually enforced is 6 minutes.  The system
            /// periodically checks to determine whether the sum of the user-mode execution
            /// time for all processes is greater than this end-of-job limit. If it is, the
            /// action specified in the EndOfJobTimeAction member of the
            /// JOBOBJECT_END_OF_JOB_TIME_INFORMATION structure is carried out. By default,
            /// all processes are terminated and the status code is set to
            /// ERROR_NOT_ENOUGH_QUOTA.
            /// </summary>
            [FieldOffset(8)]
            public long PerJobUserTimeLimit;

            /// <summary>
            /// Limit flags that are in effect. This member is a bit field that determines
            /// whether other structure members are used. Any combination LimitFlag values
            /// can be specified.
            /// </summary>
            [FieldOffset(16)]
            public LimitFlags LimitFlags;

            /// <summary>
            /// If LimitFlags specifies JOB_OBJECT_LIMIT_WORKINGSET, this member is the
            /// minimum working set size for each process associated with the job. Otherwise,
            /// this member is ignored.  If MaximumWorkingSetSize is nonzero,
            /// MinimumWorkingSetSize cannot be zero.
            /// </summary>
            [FieldOffset(20)]
            public uint MinimumWorkingSetSize;

            /// <summary>
            /// If LimitFlags specifies JOB_OBJECT_LIMIT_WORKINGSET, this member is the
            /// maximum working set size for each process associated with the job. Otherwise,
            /// this member is ignored.  If MinimumWorkingSetSize is nonzero,
            /// MaximumWorkingSetSize cannot be zero.
            /// </summary>
            [FieldOffset(24)]
            public uint MaximumWorkingSetSize;

            /// <summary>
            /// If LimitFlags specifies JOB_OBJECT_LIMIT_ACTIVE_PROCESS, this member is the
            /// active process limit for the job. Otherwise, this member is ignored.  If you
            /// try to associate a process with a job, and this causes the active process
            /// count to exceed this limit, the process is terminated and the association
            /// fails.
            /// </summary>
            [FieldOffset(28)]
            public int ActiveProcessLimit;

            /// <summary>
            /// If LimitFlags specifies JOB_OBJECT_LIMIT_AFFINITY, this member is the
            /// processor affinity for all processes associated with the job. Otherwise,
            /// this member is ignored.  The affinity must be a proper subset of the system
            /// affinity mask obtained by calling the GetProcessAffinityMask function. The
            /// affinity of each thread is set to this value, but threads are free to
            /// subsequently set their affinity, as long as it is a subset of the specified
            /// affinity mask. Processes cannot set their own affinity mask.
            /// </summary>
            [FieldOffset(32)]
            public IntPtr Affinity;

            /// <summary>
            /// If LimitFlags specifies JOB_OBJECT_LIMIT_PRIORITY_CLASS, this member is the
            /// priority class for all processes associated with the job. Otherwise, this
            /// member is ignored. Processes and threads cannot modify their priority class.
            /// The calling process must enable the SE_INC_BASE_PRIORITY_NAME privilege.
            /// </summary>
            [FieldOffset(36)]
            public int PriorityClass;

            /// <summary>
            /// If LimitFlags specifies JOB_OBJECT_LIMIT_SCHEDULING_CLASS, this member is
            /// the scheduling class for all processes associated with the job. Otherwise,
            /// this member is ignored.  The valid values are 0 to 9. Use 0 for the least
            /// favorable scheduling class relative to other threads, and 9 for the most
            /// favorable scheduling class relative to other threads. By default, this
            /// value is 5. To use a scheduling class greater than 5, the calling process
            /// must enable the SE_INC_BASE_PRIORITY_NAME privilege.
            /// </summary>
            [FieldOffset(40)]
            public int SchedulingClass;
        }

        /// <summary>
        /// The JOBOBJECT_BASIC_LIMIT_INFORMATION structure contains basic limit
        /// information for a job object.
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        private struct BasicLimits64
        {
            /// <summary>
            /// If LimitFlags specifies JOB_OBJECT_LIMIT_PROCESS_TIME, this member is
            /// the per-process user-mode execution time limit, in 100-nanosecond ticks.
            /// Otherwise, this member is ignored.  The system periodically checks to
            /// determine whether each process associated with the job has accumulated
            /// more user-mode time than the set limit. If it has, the process is terminated.
            /// </summary>
            [FieldOffset(0)]
            public long PerProcessUserTimeLimit;

            /// <summary>
            /// If LimitFlags specifies JOB_OBJECT_LIMIT_JOB_TIME, this member is the
            /// per-job user-mode execution time limit, in 100-nanosecond ticks. Otherwise,
            /// this member is ignored. The system adds the current time of the processes
            /// associated with the job to this limit. For example, if you set this limit
            /// to 1 minute, and the job has a process that has accumulated 5 minutes of
            /// user-mode time, the limit actually enforced is 6 minutes.  The system
            /// periodically checks to determine whether the sum of the user-mode execution
            /// time for all processes is greater than this end-of-job limit. If it is, the
            /// action specified in the EndOfJobTimeAction member of the
            /// JOBOBJECT_END_OF_JOB_TIME_INFORMATION structure is carried out. By default,
            /// all processes are terminated and the status code is set to
            /// ERROR_NOT_ENOUGH_QUOTA.
            /// </summary>
            [FieldOffset(8)]
            public long PerJobUserTimeLimit;

            /// <summary>
            /// Limit flags that are in effect. This member is a bit field that determines
            /// whether other structure members are used. Any combination LimitFlag values
            /// can be specified.
            /// </summary>
            [FieldOffset(16)]
            public LimitFlags LimitFlags;

            /// <summary>
            /// If LimitFlags specifies JOB_OBJECT_LIMIT_WORKINGSET, this member is the
            /// minimum working set size for each process associated with the job. Otherwise,
            /// this member is ignored.  If MaximumWorkingSetSize is nonzero,
            /// MinimumWorkingSetSize cannot be zero.
            /// </summary>
            [FieldOffset(24)]
            public ulong MinimumWorkingSetSize;

            /// <summary>
            /// If LimitFlags specifies JOB_OBJECT_LIMIT_WORKINGSET, this member is the
            /// maximum working set size for each process associated with the job. Otherwise,
            /// this member is ignored.  If MinimumWorkingSetSize is nonzero,
            /// MaximumWorkingSetSize cannot be zero.
            /// </summary>
            [FieldOffset(32)]
            public ulong MaximumWorkingSetSize;

            /// <summary>
            /// If LimitFlags specifies JOB_OBJECT_LIMIT_ACTIVE_PROCESS, this member is the
            /// active process limit for the job. Otherwise, this member is ignored.  If you
            /// try to associate a process with a job, and this causes the active process
            /// count to exceed this limit, the process is terminated and the association
            /// fails.
            /// </summary>
            [FieldOffset(40)]
            public int ActiveProcessLimit;

            /// <summary>
            /// If LimitFlags specifies JOB_OBJECT_LIMIT_AFFINITY, this member is the
            /// processor affinity for all processes associated with the job. Otherwise,
            /// this member is ignored.  The affinity must be a proper subset of the system
            /// affinity mask obtained by calling the GetProcessAffinityMask function. The
            /// affinity of each thread is set to this value, but threads are free to
            /// subsequently set their affinity, as long as it is a subset of the specified
            /// affinity mask. Processes cannot set their own affinity mask.
            /// </summary>
            [FieldOffset(48)]
            public IntPtr Affinity;

            /// <summary>
            /// If LimitFlags specifies JOB_OBJECT_LIMIT_PRIORITY_CLASS, this member is the
            /// priority class for all processes associated with the job. Otherwise, this
            /// member is ignored. Processes and threads cannot modify their priority class.
            /// The calling process must enable the SE_INC_BASE_PRIORITY_NAME privilege.
            /// </summary>
            [FieldOffset(56)]
            public int PriorityClass;

            /// <summary>
            /// If LimitFlags specifies JOB_OBJECT_LIMIT_SCHEDULING_CLASS, this member is
            /// the scheduling class for all processes associated with the job. Otherwise,
            /// this member is ignored.  The valid values are 0 to 9. Use 0 for the least
            /// favorable scheduling class relative to other threads, and 9 for the most
            /// favorable scheduling class relative to other threads. By default, this
            /// value is 5. To use a scheduling class greater than 5, the calling process
            /// must enable the SE_INC_BASE_PRIORITY_NAME privilege.
            /// </summary>
            [FieldOffset(60)]
            public int SchedulingClass;
        }
        #endregion

        #region ExtendedLimits Structures

        /// <summary>
        /// The JOBOBJECT_EXTENDED_LIMIT_INFORMATION structure contains basic and extended limit
        /// information for a job object.
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        private struct ExtendedLimits32
        {
            /// <summary>
            /// A JOBOBJECT_BASIC_LIMIT_INFORMATION structure that contains
            /// basic limit information.
            /// </summary>
            [FieldOffset(0)]
            public BasicLimits32 BasicLimits;

            /// <summary>
            /// Resereved.
            /// </summary>
            [FieldOffset(48)]
            public IoCounters32 IoInfo;

            /// <summary>
            /// If the LimitFlags member of the JOBOBJECT_BASIC_LIMIT_INFORMATION structure
            /// specifies the JOB_OBJECT_LIMIT_PROCESS_MEMORY value, this member specifies
            /// the limit for the virtual memory that can be committed by a process.
            /// Otherwise, this member is ignored.
            /// </summary>
            [FieldOffset(96)]
            public uint ProcessMemoryLimit;

            /// <summary>
            /// If the LimitFlags member of the JOBOBJECT_BASIC_LIMIT_INFORMATION structure
            /// specifies the JOB_OBJECT_LIMIT_JOB_MEMORY value, this member specifies the
            /// limit for the virtual memory that can be committed for the job. Otherwise,
            /// this member is ignored.
            /// </summary>
            [FieldOffset(100)]
            public uint JobMemoryLimit;

            /// <summary>
            /// Peak memory used by any process ever associated with the job.
            /// </summary>
            [FieldOffset(104)]
            public uint PeakProcessMemoryUsed;

            /// <summary>
            /// Peak memory usage of all processes currently associated with the job.
            /// </summary>
            [FieldOffset(108)]
            public uint PeakJobMemoryUsed;
        }

        /// <summary>
        /// The JOBOBJECT_EXTENDED_LIMIT_INFORMATION structure contains basic and extended limit
        /// information for a job object.
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        private struct ExtendedLimits64
        {
            /// <summary>
            /// A JOBOBJECT_BASIC_LIMIT_INFORMATION structure that contains
            /// basic limit information.
            /// </summary>
            [FieldOffset(0)]
            public BasicLimits64 BasicLimits;

            /// <summary>
            /// Resereved.
            /// </summary>
            [FieldOffset(64)]
            public IoCounters64 IoInfo;

            /// <summary>
            /// If the LimitFlags member of the JOBOBJECT_BASIC_LIMIT_INFORMATION structure
            /// specifies the JOB_OBJECT_LIMIT_PROCESS_MEMORY value, this member specifies
            /// the limit for the virtual memory that can be committed by a process.
            /// Otherwise, this member is ignored.
            /// </summary>
            [FieldOffset(112)]
            public ulong ProcessMemoryLimit;

            /// <summary>
            /// If the LimitFlags member of the JOBOBJECT_BASIC_LIMIT_INFORMATION structure
            /// specifies the JOB_OBJECT_LIMIT_JOB_MEMORY value, this member specifies the
            /// limit for the virtual memory that can be committed for the job. Otherwise,
            /// this member is ignored.
            /// </summary>
            [FieldOffset(120)]
            public ulong JobMemoryLimit;

            /// <summary>
            /// Peak memory used by any process ever associated with the job.
            /// </summary>
            [FieldOffset(128)]
            public ulong PeakProcessMemoryUsed;

            /// <summary>
            /// Peak memory usage of all processes currently associated with the job.
            /// </summary>
            [FieldOffset(136)]
            public ulong PeakJobMemoryUsed;
        }
        #endregion

        #region JobObjectInfo Union

        /// <summary>
        /// Union of different limit data structures that may be passed
        /// to SetInformationJobObject / from QueryInformationJobObject.
        /// This union also contains separate 32 and 64 bit versions of
        /// each structure.
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        private struct JobObjectInfo
        {
            #region 32 bit structures

            /// <summary>
            /// The BasicLimits32 structure contains basic limit information
            /// for a job object on a 32bit platform.
            /// </summary>
            [FieldOffset(0)]
            public BasicLimits32 basicLimits32;

            /// <summary>
            /// The ExtendedLimits32 structure contains extended limit information
            /// for a job object on a 32bit platform.
            /// </summary>
            [FieldOffset(0)]
            public ExtendedLimits32 extendedLimits32;

            #endregion

            #region 64 bit structures

            /// <summary>
            /// The BasicLimits64 structure contains basic limit information
            /// for a job object on a 64bit platform.
            /// </summary>
            [FieldOffset(0)]
            public BasicLimits64 basicLimits64;

            /// <summary>
            /// The ExtendedLimits64 structure contains extended limit information
            /// for a job object on a 64bit platform.
            /// </summary>
            [FieldOffset(0)]
            public ExtendedLimits64 extendedLimits64;

            #endregion
        }

        #endregion

        #region WinAPI Class

        /// <summary>
        /// Private class that holds all the Windows API calls made by this
        /// </summary>
        private class WinAPI
        {
            /// <summary>
            /// The CreateJobObject function creates or opens a job object.
            /// </summary>
            /// <param name="jobAttributes">Pointer to a SECURITY_ATTRIBUTES structure</param> 
            /// <param name="name"> Pointer to a null-terminated string specifying the name of the job. </param>
            /// <returns>If the function succeeds, the return value is a handle to the job object</returns>
            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern IntPtr CreateJobObject(IntPtr jobAttributes, string name);

            /// <summary>
            /// The AssignProcessToJobObject function assigns a process to an existing job object.
            /// </summary>
            /// <param name="jobHandle">Handle to the job object to which the process will be associated.  </param>
            /// <param name="processHandle">Handle to the process to associate with the job object </param>
            /// <returns>If the function succeeds, the return value is nonzero </returns>
            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool AssignProcessToJobObject(IntPtr jobHandle, IntPtr processHandle);

            /// <summary>
            /// The SetInformationJobObject function sets limits for a job object.
            /// </summary>
            /// <param name="jobHandle">Handle to the job whose limits are being set.</param>
            /// <param name="jobObjectInfoClass">Information class for the limits to be set. This
            /// parameter can be one of the following values.</param>
            /// <param name="jobObjectInfo">Limits to be set for the job. The format of this data
            /// depends on the value of JobObjectInfoClass.</param>
            /// <param name="jobObjectInfoLength">Size of the job information being set, in
            /// bytes.</param>
            /// <returns>If the function succeeds, the return value is nonzero.  If the function
            /// fails, the return value is zero. To get extended error information,
            /// call GetLastError.</returns>
            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool SetInformationJobObject(
              [In] IntPtr jobHandle,
              [In] JobObjectInfoClass jobObjectInfoClass,
              [In] ref JobObjectInfo jobObjectInfo,
              [In] int jobObjectInfoLength);

            /// <summary>
            /// The CloseHandle function lets us destroy a JobObject handle.
            /// </summary>
            /// <param name="jobHandle">Handle to the job</param>
            /// <returns>If the function succeeds, the return value true.  If the function
            /// fails, the return value is false. To get extended error information,
            /// call GetLastError.</returns>
            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool CloseHandle([In] IntPtr jobHandle);
        }

        #endregion

        #region IDisposable implementation

        /// <summary>
        /// Cleanup
        /// </summary>
        ~ProcessJobObject()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Helper function to dispose managed and unmanaged resources
        /// </summary>
        /// <param name="disposing"></param>
        private void Dispose(bool disposing)
        {
            bool isDisposed = this.disposed;
            if (!isDisposed)
            {
                this.disposed = true;
                if (disposing)
                {
                    // Managed resources
                }

                if (this.jobHandle != IntPtr.Zero)
                {
                    WinAPI.CloseHandle(this.jobHandle);
                }
            }
        }

        /// <summary>
        /// Dispose the resources
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
