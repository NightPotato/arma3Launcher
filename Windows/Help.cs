﻿using arma3Launcher.Effects;
using MaterialSkin;
using MaterialSkin.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace arma3Launcher.Windows
{
    public partial class Help : MaterialForm
    {
        private WindowIO windowIO;

        public Help()
        {
            // Material Skin properties
            MaterialSkinManager.AddFormToManage(this);
            MaterialSkinManager.Theme = MaterialSkinManager.Themes.DARK;
            MaterialSkinManager.ColorScheme = new ColorScheme(Primary.Grey700, Primary.Grey800, Primary.Grey500, Accent.Lime200, TextShade.WHITE);

            InitializeComponent();

            windowIO = new WindowIO(this);
        }

        private void Help_Shown(object sender, EventArgs e)
        {
            windowIO.WindowIn();
        }

        private void Help_FormClosing(object sender, FormClosingEventArgs e)
        {
            if ((string)this.Tag != "close")
            {
                windowIO.WindowOut(true);
                e.Cancel = true;
            }
            else
            {
                GC.Collect(2, GCCollectionMode.Forced);
                this.Dispose();
            }
        }
    }
}
