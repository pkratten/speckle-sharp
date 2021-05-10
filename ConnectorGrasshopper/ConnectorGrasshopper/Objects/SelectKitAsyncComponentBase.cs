﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ConnectorGrasshopper.Extras;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using GrasshopperAsyncComponent;
using Sentry;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;

namespace ConnectorGrasshopper.Objects
{
  public class SelectKitAsyncComponentBase : GH_AsyncComponent
  {
    public ISpeckleConverter Converter;

    public ISpeckleKit Kit;

    public virtual bool CanDisableConversion => true;
    
    public SelectKitAsyncComponentBase(string name, string nickname, string description, string category,
      string subCategory) : base(name, nickname, description, category, subCategory)
    {
    }

    public override void AddedToDocument(GH_Document document)
    {
      base.AddedToDocument(document);
      SetConverter();
    }

    public virtual void SetConverter()
    {
      var key = "Speckle2:kit.default.name";
      var n = Grasshopper.Instances.Settings.GetValue(key, "Objects");
      if (n == "None")
      {
        Kit = null;
        Converter = null;
        Message = "No Conversion";
        return;
      }
      try
      {
        SetConverterFromKit(n);
      }
      catch
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No default kit found on this machine.");
      }
    }

    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      try
      {
        var kits = KitManager.GetKitsWithConvertersForApp(Applications.Rhino);

        Menu_AppendSeparator(menu);
        Menu_AppendItem(menu, "Select the converter you want to use:");
        if(CanDisableConversion)
          Menu_AppendItem(menu, "Do Not Convert", (s, e) =>
          {
            var key = "Speckle2:kit.default.name";
            Grasshopper.Instances.Settings.SetValue(key, "None");
            SetConverter();
            ExpireSolution(true);
          }, true, Kit == null);
          
        foreach (var kit in kits)
        {
          Menu_AppendItem(menu, $"{kit.Name} ({kit.Description})", (s, e) => { SetConverterFromKit(kit.Name); }, true,
            kit.Name == Kit?.Name);
        }

        Menu_AppendSeparator(menu);
      }
      catch (Exception e)
      {
        Menu_AppendItem(menu, "An error occurred while fetching Kits", null, false);
      }
    }

    public virtual void SetConverterFromKit(string kitName)
    {
      if (kitName == Kit?.Name) return;
      Kit = KitManager.Kits.FirstOrDefault(k => k.Name == kitName);
      Converter = Kit.LoadConverter(Applications.Rhino);
      Converter.SetContextDocument(Rhino.RhinoDoc.ActiveDoc);
      Message = $"Using the {Kit.Name} Converter";
    }

    public override Guid ComponentGuid => new Guid("2FEE5354-0F5E-41D9-ACD3-BF376D29CCDC");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      throw new SpeckleException("Please inherit from this class, don't use SelectKitComponentBase directly",
        level: SentryLevel.Warning);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      throw new SpeckleException("Please inherit from this class, don't use SelectKitComponentBase directly",
        level: SentryLevel.Warning);
    }

    protected override void BeforeSolveInstance()
    {
      Converter?.SetContextDocument(Rhino.RhinoDoc.ActiveDoc);
      base.BeforeSolveInstance();
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      //Ensure converter document is up to date
      if (Converter == null)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "No converter was provided. Conversions are disabled.");

      }

      base.SolveInstance(DA);
    }
  }
}
