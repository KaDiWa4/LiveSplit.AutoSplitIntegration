﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using LiveSplit.Model;

namespace LiveSplit.UI.Components
{
    public class AutoSplitIntegrationComponent : IComponent
    {
        internal AutoSplitProcess AutoSplit { get; set; }

        internal bool GameTimePausing { get; set; } = false;

        internal AutoSplitIntegrationComponentSettings Settings { get; }

        internal LiveSplitState State { get; private set; }

        internal TimerModel Timer { get; private set; }

        private bool ignoreNextStart = false;
        internal bool IgnoreNextStart
        {
            get => GetAndResetBool(ref ignoreNextStart);
            set => ignoreNextStart = value;
        }

        private bool ignoreNextSplit = false;
        internal bool IgnoreNextSplit
        {
            get => GetAndResetBool(ref ignoreNextSplit);
            set => ignoreNextSplit = value;
        }

        private bool ignoreNextReset = false;
        internal bool IgnoreNextReset
        {
            get => GetAndResetBool(ref ignoreNextReset);
            set => ignoreNextReset = value;
        }

        private bool GetAndResetBool(ref bool value)
        {
            if (value)
            {
                value = false;
                return true;
            }
            return false;
        }

        public string ComponentName => "AutoSplit Integration";

        public IDictionary<string, Action> ContextMenuControls { get; private set; } = new Dictionary<string, Action>();

        public string AutoSplitPath { get; set; }

        public void DrawHorizontal(Graphics g, LiveSplitState state, float height, Region clipRegion) { }
        public void DrawVertical(Graphics g, LiveSplitState state, float height, Region clipRegion) { }
        public float HorizontalWidth => 0;
        public float MinimumHeight => 0;
        public float VerticalHeight => 0;
        public float MinimumWidth => 0;
        public float PaddingTop => 0;
        public float PaddingBottom => 0;
        public float PaddingLeft => 0;
        public float PaddingRight => 0;

        public AutoSplitIntegrationComponent(LiveSplitState state)
        {
            State = state;
            Timer = new TimerModel()
            {
                CurrentState = State
            };

            Settings = new AutoSplitIntegrationComponentSettings(this);

            State.OnStart += State_OnStart;
            State.OnSplit += State_OnSplit;
            State.OnReset += State_OnReset;
            State.OnSkipSplit += State_OnSkipSplit;
            State.OnUndoSplit += State_OnUndoSplit;
        }

        public Control GetSettingsControl(LayoutMode mode) => Settings;

        public XmlNode GetSettings(XmlDocument document) => Settings.GetSettings(document);

        public void SetSettings(XmlNode settings)
        {
            Settings.SetSettings(settings);

            StartAutoSplit();
        }

        public void Update(IInvalidator invalidator, LiveSplitState state, float width, float height, LayoutMode mode) { }

        public void StartAutoSplit()
        {
            if (!string.IsNullOrEmpty(AutoSplitPath) && File.Exists(AutoSplitPath))
            {
                AutoSplit?.Close();
                AutoSplit = new AutoSplitProcess(this);
            }
        }

        public void KillAutoSplit()
        {
            if (!AutoSplit.IsRunning)
                return;

            try
            {
                AutoSplit.MainProcess.Kill();
            }
            catch { }
        }

        public void Dispose() => AutoSplit?.Close();

        private void State_OnStart(object sender, EventArgs e)
        {
            if (IgnoreNextStart)
                return;

            AutoSplit.Send("start");
            if (GameTimePausing)
                Timer.InitializeGameTime();

            Settings.OnStart();
        }

        private void State_OnSplit(object sender, EventArgs e)
        {
            if (IgnoreNextSplit)
                return;

            AutoSplit.Send("split");
        }

        private void State_OnReset(object sender, TimerPhase e)
        {
            if (IgnoreNextReset)
                return;

            AutoSplit.Send("reset");
            Settings.OnReset();
        }

        private void State_OnSkipSplit(object sender, EventArgs e) => AutoSplit.Send("skip");

        private void State_OnUndoSplit(object sender, EventArgs e) => AutoSplit.Send("undo");
    }
}