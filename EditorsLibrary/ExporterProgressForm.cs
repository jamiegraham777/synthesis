﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EditorsLibrary
{
    public partial class ExporterProgressForm : Form
    {

        public bool finished;

        private TextWriter oldConsole;
        private TextboxWriter newConsole;

        private delegate void resetProgressDelegate();
        private delegate void addProgressDelegate(int value);
        private delegate int getProgressDelegate();
        private delegate void setProgressTextDelegate(string text);
        private delegate void finishDelegate(string logFile);

        public ExporterProgressForm(AutoResetEvent startEvent, Color textColor, Color backgroundColor)
        {
            InitializeComponent();

            oldConsole = Console.Out;

            newConsole = new TextboxWriter(logText);
            Console.SetOut(newConsole);

            logText.ForeColor = textColor;
            logText.BackColor = backgroundColor;

            label1.Text = "";

            buttonSaveLog.Enabled = false;
            buttonSaveLog.Visible = false;

            FormClosing += delegate(object sender, FormClosingEventArgs e)
            {
                Console.SetOut(oldConsole);
                startEvent.Set();
            };

            buttonStart.Click += delegate(object sender, EventArgs e)
            {
                if (!finished)
                {
                    startEvent.Set();
                    buttonStart.Enabled = false;
                }
                else Close();
            };

            Application.Idle += delegate(object sender, EventArgs e)
            {
                newConsole.printQueue();
            };
        }

        public void ResetProgress()
        {
            if (progressBar1.InvokeRequired)
            {
                progressBar1.Invoke(new resetProgressDelegate(ResetProgress));
                return;
            }

            progressBar1.Value = 0;
            label1.Text = "";
        }

        public int GetProgress()
        {
            if (progressBar1.InvokeRequired)
            {
                return (int) progressBar1.Invoke(new getProgressDelegate(GetProgress));
            }

            return progressBar1.Value;
        }

        public void AddProgress(int percentLength)
        {
            if (progressBar1.InvokeRequired)
            {
                progressBar1.Invoke(new addProgressDelegate(AddProgress), percentLength);
                return;
            }

            progressBar1.Step = percentLength;
            progressBar1.PerformStep();
        }

        public void SetProgressText(string text)
        {
            if (label1.InvokeRequired)
            {
                label1.Invoke(new setProgressTextDelegate(SetProgressText), text);
                return;
            }

            label1.Text = "Progress: " + text;
        }

        public void Finish(string logFile = null)
        {
            if (InvokeRequired)
            {
                Invoke(new finishDelegate(Finish), logFile);
                return;
            }

            label1.Text = "Finished";

            buttonSaveLog.Enabled = (logFile != null);
            buttonSaveLog.Visible = buttonSaveLog.Enabled;

            buttonStart.Text = "Close";
            buttonStart.Enabled = true;

            buttonSaveLog.Click += delegate(object sender, EventArgs e)
            {
                try
                {
                    using (StreamWriter logFileStream = new StreamWriter(logFile))
                    {
                        logFileStream.Write(logText.Text);
#if DEBUG
                        Console.WriteLine("Wrote " + logFile);
#endif
                    }
                }
                catch (IOException ie)
                {
                    Console.WriteLine(ie);
                    Console.WriteLine("Couldn't write log file " + logFile);
                }
            };

            finished = true;
        }

        public string GetLogText()
        {
            return logText.Text;
        }

        private class TextboxWriter : StringWriter
        {

            private delegate void WriteDelegate(string value);

            private RichTextBox _box;
            private Queue<string> lineQueue;

            public TextboxWriter(RichTextBox box)
            {
                _box = box;
                lineQueue = new Queue<string>();
            }

            public void printQueue()
            {
                if (lineQueue.Count == 0) return;

                string toPrint = "";

                while (lineQueue.Count > 0)
                {
                    toPrint += lineQueue.Dequeue() + NewLine;
                }

                base.WriteLine(toPrint);
                _box.AppendText(toPrint); // When character data is written, append it to the text box.
                _box.ScrollToCaret();
            }

            public override void WriteLine(string value)
            {
                if (_box.InvokeRequired)
                {
                    _box.Invoke(new WriteDelegate(WriteLine), value);
                    return;
                }

                lineQueue.Enqueue(value);
            }

            public override void Write(string value)
            {
                WriteLine(value);
            }

            public override Encoding Encoding
            {
                get { return System.Text.Encoding.UTF8; }
            }

        }

    }
}
