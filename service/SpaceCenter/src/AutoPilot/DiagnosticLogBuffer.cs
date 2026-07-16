using System.Globalization;
using System.Text;
using UnityEngine;

namespace KRPC.SpaceCenter.AutoPilot
{
    /// <summary>
    /// The attitude controller's diagnostic log: CSV, one data row per physics tick, first row a
    /// header naming every column. Vector-valued channels occupy one column per component,
    /// suffixed <c>.p/.r/.y</c> (pitch, roll, yaw — the controller's x, y, z axes); axis-group
    /// channels (pitch/yaw coupled, roll separate) are suffixed <c>.py/.roll</c>. Values use the
    /// invariant culture so the output is valid CSV regardless of the OS locale.
    ///
    /// Rows are built one field at a time (the header is accumulated from the field names while
    /// the first row is built, so names and values cannot drift apart), then committed to the
    /// in-memory buffer. The buffer is capped at <see cref="MaxDataRows"/> data rows; the caller
    /// checks <see cref="Full"/> after committing and stops logging when the cap is reached.
    ///
    /// Committed text is guarded by a lock: rows are built and committed on the physics thread,
    /// while <see cref="GetText"/> and <see cref="Clear"/> are called from RPC threads.
    /// </summary>
    sealed class DiagnosticLogBuffer
    {
        /// <summary>
        /// Maximum number of data rows (3001 rows including the header): one minute at the
        /// 50 Hz physics rate.
        /// </summary>
        public const int MaxDataRows = 3000;

        readonly StringBuilder text = new StringBuilder ();
        readonly StringBuilder header = new StringBuilder ();
        readonly StringBuilder row = new StringBuilder ();
        readonly object sync = new object ();
        bool headerWritten;
        int dataRows;

        public bool Full {
            get {
                lock (sync) {
                    return dataRows >= MaxDataRows;
                }
            }
        }

        public void Clear ()
        {
            lock (sync) {
                text.Length = 0;
                header.Length = 0;
                row.Length = 0;
                headerWritten = false;
                dataRows = 0;
            }
        }

        public string GetText ()
        {
            lock (sync) {
                return text.ToString ();
            }
        }

        public void Add (string name, string value)
        {
            if (!headerWritten) {
                if (header.Length > 0)
                    header.Append (',');
                header.Append (name);
            }
            if (row.Length > 0)
                row.Append (',');
            row.Append (value);
        }

        public void Add (string name, double value, string format)
        {
            Add (name, value.ToString (format, CultureInfo.InvariantCulture));
        }

        public void Add (string name, bool value)
        {
            Add (name, value ? "1" : "0");
        }

        public void Add (string name, int value)
        {
            Add (name, value.ToString (CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// One column per component, named <c>name.p/.r/.y</c> (pitch, roll, yaw — the
        /// vector's x, y, z components in the controller's axis convention).
        /// </summary>
        public void AddVector (string name, Vector3d value, string format)
        {
            Add (name + ".p", value.x, format);
            Add (name + ".r", value.y, format);
            Add (name + ".y", value.z, format);
        }

        /// <summary>One column per axis group, named <c>name.py/.roll</c>.</summary>
        public void AddGroup (string name, double pitchYaw, double roll, string format)
        {
            Add (name + ".py", pitchYaw, format);
            Add (name + ".roll", roll, format);
        }

        /// <summary>
        /// Commit the row built by the <c>Add</c> calls since the last commit. On the first
        /// commit after a <see cref="Clear"/> the header row is emitted first and returned via
        /// <paramref name="headerLine"/> (null on subsequent commits) so the caller can echo it
        /// to the game log alongside the returned data row.
        /// </summary>
        public string CommitRow (out string headerLine)
        {
            headerLine = null;
            if (!headerWritten) {
                headerLine = header.ToString ();
                headerWritten = true;
            }
            var line = row.ToString ();
            row.Length = 0;
            lock (sync) {
                if (headerLine != null)
                    text.AppendLine (headerLine);
                text.AppendLine (line);
                dataRows++;
            }
            return line;
        }
    }
}
