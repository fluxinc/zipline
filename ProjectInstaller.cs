﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.Threading.Tasks;

namespace DICOMCapacitorWarden
{
  [RunInstaller(true)]
  public partial class ProjectInstaller : Installer
  {
    public ProjectInstaller()
    {
      InitializeComponent();
    }

    private void serviceProcessInstaller1_AfterInstall(object sender, InstallEventArgs e)
    {

    }
  }
}
