﻿using System;

namespace FamiStudio
{
    class LogDialog : ILogOutput
    {
        public LogDialog(FamiStudioWindow parentForm)
        {
        }

        public DialogResult ShowDialog()
        {
            return DialogResult.OK;
        }

        public DialogResult ShowDialogIfMessages()
        {
            return DialogResult.OK;
        }

        public void LogMessage(string msg)
        {
        }

        public bool HasMessages => false;
        public bool AbortOperation => false;
        public void ReportProgress(float progress) { }
    }

    class LogProgressDialog : ILogOutput
    {
        bool shown = false;
        bool abort = false;
        bool hasMessages = false;
        private float lastProgress;
        private PropertyDialog dialog;
        private FamiStudioWindow parentForm;

        public unsafe LogProgressDialog(FamiStudioWindow parentForm, string title, string text)
        {
            this.parentForm = parentForm;
            
            Platform.InvokeOnMainThread(() =>
            {
                // HACK : We only use this for video export on mobile.
                dialog = new PropertyDialog(parentForm, title, 100, false);
                //dialog.Properties.AddProgressBar("Export progress", 0.0f, text); // 0 MATTT
                dialog.Properties.AddProgressBar("Export progress"); 
                dialog.Properties.AddLabel("Current Step", " "); // 1
                dialog.Properties.Build();
            });
        }

        public void LogMessage(string msg)
        {
            Platform.InvokeOnMainThread(() =>
            {
                dialog.Properties.SetPropertyValue(1, msg);
            });
        }

        public void ReportProgress(float progress)
        {
            // Avoid bombarding the main thread with tiny updates, slows down everything.
            if (Math.Abs(lastProgress - progress) > 0.1f)
            {
                Platform.InvokeOnMainThread(() =>
                {
                    if (!shown)
                    {
                        shown = true;
                        dialog.ShowDialogAsync((r) => { abort = r != DialogResult.None; });
                    }

                    dialog.Properties.SetPropertyValue(0, progress);
                });

                lastProgress = progress;
            }
        }

        public void StayModalUntilClosed()
        {
        }

        public void Close()
        {
            Platform.InvokeOnMainThread(() =>
            {
                dialog.Close(DialogResult.OK);
            });
        }

        public bool HasMessages => false;
        public bool AbortOperation => abort;
    }
}
