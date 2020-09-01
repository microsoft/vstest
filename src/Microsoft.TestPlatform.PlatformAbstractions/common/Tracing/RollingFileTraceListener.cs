// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETFRAMEWORK || NETCOREAPP

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Performs logging to a file and rolls the output file when either time or size thresholds are
    /// exceeded.
    /// </summary>
    public class RollingFileTraceListener : TextWriterTraceListener
    {
        private readonly StreamWriterRollingHelper rollingHelper;

        private readonly int rollSizeInBytes;

        /// <summary>
        /// Initializes a new instance of the <see cref="RollingFileTraceListener"/> class.
        /// </summary>
        /// <param name="fileName">The filename where the entries will be logged.</param>
        /// <param name="name">Name of the trace listener.</param>
        /// <param name="rollSizeKB">The maximum file size (KB) before rolling.</param>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "the fileStream is passed into the TextWriterTraceListener")]
        public RollingFileTraceListener(
            string fileName,
            string name,
            int rollSizeKB)
            : base(OpenTextWriter(fileName), name)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException(nameof(fileName));
            }

            if (rollSizeKB <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rollSizeKB));
            }

            this.TraceFileName = fileName;
            this.rollSizeInBytes = rollSizeKB * 1024;
            this.rollingHelper = new StreamWriterRollingHelper(this);
        }

        /// <summary>
        /// Gets name of the Trace file.
        /// </summary>
        internal string TraceFileName
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the <see cref="StreamWriterRollingHelper"/> for the flat file.
        /// </summary>
        /// <value>
        /// The <see cref="StreamWriterRollingHelper"/> for the flat file.
        /// </value>
        internal StreamWriterRollingHelper RollingHelper
        {
            get { return this.rollingHelper; }
        }

        /// <summary>
        /// Writes the trace messages to the file.
        /// </summary>
        /// <param name="message">Trace message string</param>
        public override void WriteLine(string message)
        {
            this.rollingHelper.RollIfNecessary();
            base.WriteLine(message);
        }

        /// <summary>
        /// Opens specified file and returns text writer.
        /// </summary>
        /// <param name="fileName">The file to open.</param>
        /// <returns>A <see cref="TallyKeepingFileStreamWriter"/> instance.</returns>
        internal static TallyKeepingFileStreamWriter OpenTextWriter(string fileName)
        {
            return new TallyKeepingFileStreamWriter(
                                    File.Open(fileName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite),
                                    GetEncodingWithFallback());
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            this.rollingHelper?.Dispose();

            base.Dispose(disposing);
        }

        private static Encoding GetEncodingWithFallback()
        {
            Encoding encoding = (Encoding)new UTF8Encoding(false).Clone();
#if TODO
            encoding.EncoderFallback = EncoderFallback.ReplacementFallback;
            encoding.DecoderFallback = DecoderFallback.ReplacementFallback;
#endif
            return encoding;
        }

        /// <summary>
        /// Encapsulates the logic to perform rolls.
        /// </summary>
        /// <remarks>
        /// If no rolling behavior has been configured no further processing will be performed.
        /// </remarks>
        internal sealed class StreamWriterRollingHelper : IDisposable
        {
            /// <summary>
            /// Synchronization lock.
            /// </summary>
            private object synclock = new object();

            /// <summary>
            /// Whether the object is disposed or not.
            /// </summary>
            private bool disposed = false;

            /// <summary>
            /// A tally keeping writer used when file size rolling is configured.<para/>
            /// The original stream writer from the base trace listener will be replaced with
            /// this listener.
            /// </summary>
            private TallyKeepingFileStreamWriter managedWriter;

            /// <summary>
            /// The trace listener for which rolling is being managed.
            /// </summary>
            private RollingFileTraceListener owner;

            /// <summary>
            /// Initializes a new instance of the <see cref="StreamWriterRollingHelper"/> class.
            /// </summary>
            /// <param name="owner">
            /// The <see cref="RollingFileTraceListener"/> to use.
            /// </param>
            public StreamWriterRollingHelper(RollingFileTraceListener owner)
            {
                this.owner = owner;
                this.managedWriter = owner.Writer as TallyKeepingFileStreamWriter;
            }

            /// <summary>
            /// Checks whether rolling should be performed, and returns the date to use when performing the roll.
            /// </summary>
            /// <returns>The date roll to use if performing a roll, or <see langword="null"/> if no rolling should occur.</returns>
            /// <remarks>
            /// Defer request for the roll date until it is necessary to avoid overhead.<para/>
            /// Information used for rolling checks should be set by now.
            /// </remarks>
            public DateTime? CheckIsRollNecessary()
            {
                // check for size roll, if enabled.
                if (this.managedWriter != null && this.managedWriter.Tally > this.owner.rollSizeInBytes)
                {
                    return DateTime.Now;
                }

                // no roll is necessary, return a null roll date
                return null;
            }

            /// <summary>
            /// Perform the roll for the next date.
            /// </summary>
            /// <param name="rollDateTime">The roll date.</param>
            public void PerformRoll(DateTime rollDateTime)
            {
                string actualFileName = ((FileStream)((StreamWriter)this.owner.Writer).BaseStream).Name;

                // calculate archive name
                string directory = Path.GetDirectoryName(actualFileName);
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(actualFileName);
                string extension = Path.GetExtension(actualFileName);

                StringBuilder fileNameBuilder = new StringBuilder(fileNameWithoutExtension);
                fileNameBuilder.Append('.');
                fileNameBuilder.Append("bak");
                fileNameBuilder.Append(extension);

                string archiveFileName = Path.Combine(directory, fileNameBuilder.ToString());
#if TODO

// close file
                owner.Writer.Close();
#endif

                // move file
                SafeMove(actualFileName, archiveFileName, rollDateTime);

                // update writer - let TWTL open the file as needed to keep consistency
                this.owner.Writer = null;
                this.managedWriter = null;
                this.UpdateRollingInformationIfNecessary();
            }

            /// <summary>
            /// Rolls the file if necessary.
            /// </summary>
            public void RollIfNecessary()
            {
                if (!this.UpdateRollingInformationIfNecessary())
                {
                    // an error was detected while handling roll information - avoid further processing
                    return;
                }

                DateTime? rollDateTime;
                if ((rollDateTime = this.CheckIsRollNecessary()) != null)
                {
                    lock (this.synclock)
                    {
                        // double check if the roll is still required and do it...
                        if ((rollDateTime = this.CheckIsRollNecessary()) != null)
                        {
                            this.PerformRoll(rollDateTime.Value);
                        }
                    }
                }
            }

            /// <summary>
            /// Updates book keeping information necessary for rolling, as required by the specified
            /// rolling configuration.
            /// </summary>
            /// <returns>true if update was successful, false if an error occurred.</returns>
            public bool UpdateRollingInformationIfNecessary()
            {
                // replace writer with the tally keeping version if necessary for size rolling
                if (this.managedWriter == null)
                {
                    try
                    {
                        this.managedWriter = OpenTextWriter(this.owner.TraceFileName);
                    }
                    catch (Exception)
                    {
                        // there's a slight chance of error here - abort if this occurs and just let TWTL handle it without attempting to roll
                        return false;
                    }

                    this.owner.Writer = this.managedWriter;
                }

                return true;
            }

            /// <summary>
            /// Disposes this instance
            /// </summary>
            public void Dispose()
            {
                this.Dispose(true);
                GC.SuppressFinalize(this);
            }

            private static void SafeMove(string actualFileName, string archiveFileName, DateTime currentDateTime)
            {
                try
                {
                    if (File.Exists(archiveFileName))
                    {
                        File.Delete(archiveFileName);
                    }

                    // take care of tunneling issues http://support.microsoft.com/kb/172190
                    File.SetCreationTime(actualFileName, currentDateTime);
                    File.Move(actualFileName, archiveFileName);
                }
                catch (IOException)
                {
                    // catch errors and attempt move to a new file with a GUID
                    archiveFileName = archiveFileName + Guid.NewGuid().ToString();

                    try
                    {
                        File.Move(actualFileName, archiveFileName);
                    }
                    catch (IOException)
                    {
                    }
                }
            }

            private void Dispose(bool disposing)
            {
                if (!this.disposed)
                {
                    if (disposing && this.managedWriter != null)
                    {
#if TODO
                        managedWriter.Close();
#endif
                    }

                    this.disposed = true;
                }
            }
        }

        /// <summary>
        /// Represents a file stream writer that keeps a tally of the length of the file.
        /// </summary>
        internal sealed class TallyKeepingFileStreamWriter : StreamWriter
        {
            private long tally;

            /// <summary>
            /// Initializes a new instance of the <see cref="TallyKeepingFileStreamWriter"/> class.
            /// </summary>
            /// <param name="stream">
            /// The <see cref="FileStream"/> to write to.
            /// </param>
            public TallyKeepingFileStreamWriter(FileStream stream)
                : base(stream)
            {
                if (stream == null)
                {
                    throw new ArgumentNullException(nameof(stream));
                }

                this.tally = stream.Length;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="TallyKeepingFileStreamWriter"/> class.
            /// </summary>
            /// <param name="stream">
            /// The <see cref="FileStream"/> to write to.
            /// </param>
            /// <param name="encoding">
            /// The <see cref="Encoding"/> to use.
            /// </param>
            public TallyKeepingFileStreamWriter(FileStream stream, Encoding encoding)
                : base(stream, encoding)
            {
                if (stream == null)
                {
                    throw new ArgumentNullException(nameof(stream));
                }

                if (encoding == null)
                {
                    throw new ArgumentNullException(nameof(encoding));
                }

                this.tally = stream.Length;
            }

            /// <summary>
            /// Gets the tally of the length of the string.
            /// </summary>
            /// <value>
            /// The tally of the length of the string.
            /// </value>
            public long Tally
            {
                get { return this.tally; }
            }

            /// <summary>
            /// Writes a character to the stream.
            /// </summary>
            /// <param name="value">
            /// The character to write to the text stream.
            /// </param>
            /// <exception cref="T:System.ObjectDisposedException">
            /// <see cref="P:System.IO.StreamWriter.AutoFlush"></see>is true or the<see cref="T:System.IO.StreamWriter"></see>buffer is full, and current writer is closed.
            /// </exception>
            /// <exception cref="T:System.NotSupportedException">
            /// <see cref="P:System.IO.StreamWriter.AutoFlush"></see>is true or the<see cref="T:System.IO.StreamWriter"></see>buffer is full, and the contents of the buffer cannot be written to the underlying fixed size stream because the<see cref="T:System.IO.StreamWriter"></see>is at the end the stream.
            /// </exception>
            /// <exception cref="T:System.IO.IOException">
            /// An I/O error occurs.
            /// </exception>
            /// <filterpriority>1</filterpriority>
            public override void Write(char value)
            {
                base.Write(value);
                this.tally += this.Encoding.GetByteCount(new[] { value });
            }

            /// <summary>
            /// Writes a character array to the stream.
            /// </summary>
            /// <param name="buffer">
            /// A character array containing the data to write. If buffer is null, nothing is written.
            /// </param>
            /// <exception cref="T:System.ObjectDisposedException">
            /// <see cref="P:System.IO.StreamWriter.AutoFlush"></see>is true or the<see cref="T:System.IO.StreamWriter"></see>buffer is full, and current writer is closed.
            /// </exception>
            /// <exception cref="T:System.NotSupportedException">
            /// <see cref="P:System.IO.StreamWriter.AutoFlush"></see>is true or the<see cref="T:System.IO.StreamWriter"></see>buffer is full, and the contents of the buffer cannot be written to the underlying fixed size stream because the<see cref="T:System.IO.StreamWriter"></see>is at the end the stream.
            /// </exception>
            /// <exception cref="T:System.IO.IOException">
            /// An I/O error occurs.
            /// </exception>
            /// <filterpriority>1</filterpriority>
            public override void Write(char[] buffer)
            {
                base.Write(buffer);
                this.tally += this.Encoding.GetByteCount(buffer);
            }

            /// <summary>
            /// Writes an array of characters to the stream.
            /// </summary>
            /// <param name="buffer">
            /// A character array containing the data to write.
            /// </param>
            /// <param name="index">
            /// The index into buffer at which to begin writing.
            /// </param>
            /// <param name="count">
            /// The number of characters to read from buffer.
            /// </param>
            /// <exception cref="T:System.IO.IOException">
            /// An I/O error occurs.
            /// </exception>
            /// <exception cref="T:System.ObjectDisposedException">
            /// <see cref="P:System.IO.StreamWriter.AutoFlush"></see>is true or the<see cref="T:System.IO.StreamWriter"></see>buffer is full, and current writer is closed.
            /// </exception>
            /// <exception cref="T:System.NotSupportedException">
            /// <see cref="P:System.IO.StreamWriter.AutoFlush"></see>is true or the<see cref="T:System.IO.StreamWriter"></see>buffer is full, and the contents of the buffer cannot be written to the underlying fixed size stream because the<see cref="T:System.IO.StreamWriter"></see>is at the end the stream.
            /// </exception>
            /// <exception cref="T:System.ArgumentOutOfRangeException">
            /// index or count is negative.
            /// </exception>
            /// <exception cref="T:System.ArgumentException">
            /// The buffer length minus index is less than count.
            /// </exception>
            /// <exception cref="T:System.ArgumentNullException">
            /// buffer is null.
            /// </exception>
            /// <filterpriority>1</filterpriority>
            public override void Write(char[] buffer, int index, int count)
            {
                base.Write(buffer, index, count);
                this.tally += this.Encoding.GetByteCount(buffer, index, count);
            }

            /// <summary>
            /// Writes a string to the stream.
            /// </summary>
            /// <param name="value">
            /// The string to write to the stream. If value is null, nothing is written.
            /// </param>
            /// <exception cref="T:System.ObjectDisposedException">
            /// <see cref="P:System.IO.StreamWriter.AutoFlush"></see>is true or the<see cref="T:System.IO.StreamWriter"></see>buffer is full, and current writer is closed.
            /// </exception>
            /// <exception cref="T:System.NotSupportedException">
            /// <see cref="P:System.IO.StreamWriter.AutoFlush"></see>is true or the<see cref="T:System.IO.StreamWriter"></see>buffer is full, and the contents of the buffer cannot be written to the underlying fixed size stream because the<see cref="T:System.IO.StreamWriter"></see>is at the end the stream.
            /// </exception>
            /// <exception cref="T:System.IO.IOException">
            /// An I/O error occurs.
            /// </exception>
            /// <filterpriority>1</filterpriority>
            public override void Write(string value)
            {
                base.Write(value);
                this.tally += this.Encoding.GetByteCount(value);
            }
        }
    }
}

#endif
