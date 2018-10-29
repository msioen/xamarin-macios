﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace xharness
{
	public class TodayExtensionTarget : UnifiedTarget
	{
		public string AppName { get; private set; }
		public string ExtensionName { get; private set; }

		public string TodayContainerGuid { get; private set; }
		public string TodayExtensionGuid { get; private set; }

		public string TodayContainerProjectPath { get; private set; }
		public string TodayExtensionProjectPath { get; private set; }

		public override string Suffix {
			get {
				return MonoNativeInfo != null ? MonoNativeInfo.FlavorSuffix + "-today" : "-today";
			}
		}

		public override string ProjectFileSuffix {
			get {
				if (MonoNativeInfo != null)
					return MonoNativeInfo.FlavorSuffix + "-today";
				return "-today";
			}
		}

		protected override void CalculateName ()
		{
			base.CalculateName ();
			if (MonoNativeInfo != null)
				Name = Name + MonoNativeInfo.FlavorSuffix;
		}

		protected override string GetMinimumOSVersion (string templateMinimumOSVersion)
		{
			if (MonoNativeInfo != null)
				return MonoNativeInfo.GetMinimumOSVersion ();
			return templateMinimumOSVersion;
		}

		void CreateTodayContainerProject ()
		{
			var csproj = new XmlDocument ();
			var suffix = Suffix;
			csproj.LoadWithoutNetworkAccess (Path.Combine (Harness.TodayContainerTemplate, "TodayContainer.csproj"));
			csproj.SetOutputPath ("bin\\$(Platform)\\$(Configuration)" + suffix, false);
			csproj.SetIntermediateOutputPath ("obj\\$(Platform)\\$(Configuration)" + suffix);
			csproj.SetProjectReferenceValue ("TodayExtension.csproj", "Name", Path.GetFileNameWithoutExtension (TodayExtensionProjectPath));
			csproj.SetProjectReferenceValue ("TodayExtension.csproj", "Project", TodayExtensionGuid);
			csproj.SetProjectReferenceInclude ("TodayExtension.csproj", TodayExtensionProjectPath.Replace ('/', '\\'));
			csproj.FixCompileInclude ("Main.cs", Path.Combine (Harness.TodayContainerTemplate, "Main.cs").Replace ('/', '\\'));
			csproj.FixInfoPListInclude (suffix);
			TodayContainerGuid = "{" + Harness.NewStableGuid ().ToString ().ToUpper () + "}";
			ProjectGuid = TodayContainerGuid;
			csproj.SetProjectGuid (TodayContainerGuid);
			Harness.Save (csproj, TodayContainerProjectPath);

			XmlDocument info_plist = new XmlDocument ();
			var target_info_plist = Path.Combine (TargetDirectory, $"Info{suffix}.plist");
			info_plist.LoadWithoutNetworkAccess (Path.Combine (Harness.TodayContainerTemplate, "Info.plist"));
			info_plist.SetCFBundleIdentifier (BundleIdentifier);
			info_plist.SetCFBundleName (Name);
			info_plist.SetMinimumOSVersion ("8.0");
			Harness.Save (info_plist, target_info_plist);
		}

		void CreateTodayExtensionProject ()
		{
			var csproj = inputProject;
			var suffix = Suffix + "-extension";
			csproj.SetProjectTypeGuids ("{EE2C853D-36AF-4FDB-B1AD-8E90477E2198};" + LanguageGuid);
			csproj.SetOutputPath ("bin\\$(Platform)\\$(Configuration)" + suffix);
			csproj.SetIntermediateOutputPath ("obj\\$(Platform)\\$(Configuration)" + suffix);
			csproj.SetImport (IsFSharp ? "$(MSBuildExtensionsPath)\\Xamarin\\iOS\\Xamarin.iOS.AppExtension.FSharp.targets" : "$(MSBuildExtensionsPath)\\Xamarin\\iOS\\Xamarin.iOS.AppExtension.CSharp.targets");
			csproj.FixInfoPListInclude (suffix);
			csproj.SetOutputType ("Library");
			csproj.AddAdditionalDefines ("XAMCORE_2_0;XAMCORE_3_0;TODAY_EXTENSION");
			var ext = IsFSharp ? "fs" : "cs";
			csproj.AddCompileInclude ("TodayExtensionMain." + ext, Path.Combine (Harness.TodayExtensionTemplate, "TodayExtensionMain." + ext), true);
			csproj.AddInterfaceDefinition (Path.Combine (Harness.TodayExtensionTemplate, "TodayView.storyboard").Replace ('/', '\\'));
			csproj.SetExtraLinkerDefs ("extra-linker-defs" + Suffix + ".xml");
			csproj.FixProjectReferences ("-today");

			Harness.Save (csproj, TodayExtensionProjectPath);

			CreateExtraLinkerDefs (Suffix);

			TodayExtensionGuid = csproj.GetProjectGuid ();

			XmlDocument info_plist = new XmlDocument ();
			var target_info_plist = Path.Combine (TargetDirectory, $"Info{suffix}.plist");
			info_plist.LoadWithoutNetworkAccess (Path.Combine (TargetDirectory, "Info.plist"));
			BundleIdentifier = info_plist.GetCFBundleIdentifier () + "-today";
			info_plist.SetCFBundleIdentifier (BundleIdentifier + ".todayextension");
			info_plist.SetMinimumOSVersion ("8.0");
			info_plist.AddPListStringValue ("CFBundlePackageType", "XPC!");
			info_plist.SetCFBundleDisplayName (Name);
			info_plist.AddPListKeyValuePair ("NSExtension", "dict", 
@"
        <key>NSExtensionMainStoryboard</key>
        <string>TodayView</string>
        <key>NSExtensionPointIdentifier</key>
        <string>com.apple.widget-extension</string>
    ");
			Harness.Save (info_plist, target_info_plist);
		}

		void CreateExtraLinkerDefs (string suffix)
		{
			var linker_defs = Path.Combine (TargetDirectory, "extra-linker-defs.xml");
			var target_linker_defs = Path.Combine (TargetDirectory, "extra-linker-defs" + suffix + ".xml");
			if (File.Exists (linker_defs) && !File.Exists (target_linker_defs)) {
				XmlDocument linker_defs_doc = new XmlDocument ();
				linker_defs_doc.LoadWithoutNetworkAccess (linker_defs);
				Harness.Save (linker_defs_doc, target_linker_defs);
			}
		}

		protected override void ExecuteInternal ()
		{
			ExtensionName = Name + " Today Extension";
			AppName = Name + " Today";

			var templateName = Path.GetFileName (TemplateProjectPath);
			if (templateName.EndsWith (".template", StringComparison.OrdinalIgnoreCase))
				templateName = Path.GetFileNameWithoutExtension (templateName);
			templateName = Path.GetFileNameWithoutExtension (templateName);

			switch (OutputType) {
			case "Exe":
				TodayExtensionProjectPath = Path.Combine (TargetDirectory, templateName + Suffix + "-extension." + ProjectFileExtension);
				TodayContainerProjectPath = Path.Combine (TargetDirectory, templateName + Suffix + "." + ProjectFileExtension);
				CreateTodayExtensionProject ();
				CreateTodayContainerProject ();
				break;
			case "Library":
				CreateLibraryProject ();
				break;
			default:
				throw new Exception (string.Format ("Unknown OutputType: {0}", OutputType));
			}
		}

		public override IEnumerable<RelatedProject> GetRelatedProjects ()
		{
			return new RelatedProject [] {
				new RelatedProject { Guid = TodayExtensionGuid, ProjectPath = TodayExtensionProjectPath },
			};
		}
	}
}
