using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using SuperMap.Data;
using SuperMap.Realspace;
using SuperMap.UI;

namespace BlocksCraft
{
    public partial class Form1 : Form
    {
        Workspace ws;
        SceneControl scon;
        public Form1()
        {
            InitializeComponent();
            init();
        }

        void init()
        {
            scon = new SceneControl();
            scon.Dock = DockStyle.Fill;
            this.Controls.Add(scon);
            string wsPath = @"D:\模型找正面\ws.smwu";
            ws = new Workspace();
            WorkspaceConnectionInfo wsCon = new WorkspaceConnectionInfo()
            {
                Server = wsPath,
                Type = WorkspaceType.SMWU
            };

            ws.Open(wsCon);

            scon.Scene.Workspace = ws;
            scon.Scene.Open(ws.Scenes[0]);

            scon.Scene.EnsureVisible(scon.Scene.Layers[0]);
        }

    }
}
