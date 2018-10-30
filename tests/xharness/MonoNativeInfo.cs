//
// MonoNativeInfo.cs
//
// Author:
//       Martin Baulig <mabaul@microsoft.com>
//
// Copyright (c) 2018 Xamarin Inc. (http://www.xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.IO;
using System.Xml;

namespace xharness
{
	public enum MonoNativeFlavor
	{
		Compat, Unified
	}

	public class MonoNativeInfo
	{
		public Harness Harness { get; }
		public MonoNativeFlavor Flavor { get; }

		public MonoNativeInfo (Harness harness, MonoNativeFlavor flavor)
		{
			Harness = harness;
			Flavor = flavor;
		}

		public string FlavorSuffix => Flavor == MonoNativeFlavor.Compat ? "-compat" : "-unified";
		public string ProjectName => "mono-native" + FlavorSuffix;
		public string ProjectPath => Path.Combine (Harness.RootDirectory, "mono-native", ProjectName + ".csproj");
		public string TemplatePath => Path.Combine (Harness.RootDirectory, "mono-native", "mono-native.csproj.template");

		public void Convert ()
		{
			return;

			var inputProject = new XmlDocument ();

			var xml = File.ReadAllText (TemplatePath);
			inputProject.LoadXmlWithoutNetworkAccess (xml);
			inputProject.SetOutputPath ("bin\\$(Platform)\\$(Configuration)" + FlavorSuffix);
			inputProject.SetIntermediateOutputPath ("obj\\$(Platform)\\$(Configuration)" + FlavorSuffix);
			inputProject.SetAssemblyName (inputProject.GetAssemblyName () + FlavorSuffix);

			switch (Flavor) {
			case MonoNativeFlavor.Compat:
				inputProject.AddAdditionalDefines ("MONO_NATIVE_COMPAT");
				break;
			case MonoNativeFlavor.Unified:
				inputProject.AddAdditionalDefines ("MONO_NATIVE_UNIFIED");
				break;
			default:
				throw new Exception ($"Unknown MonoNativeFlavor: {Flavor}");
			}

			// Harness.Save (inputProject, ProjectPath);
		}

		public void AddProjectDefines (XmlDocument project)
		{
			switch (Flavor) {
			case MonoNativeFlavor.Compat:
				project.AddAdditionalDefines ("MONO_NATIVE_COMPAT");
				break;
			case MonoNativeFlavor.Unified:
				project.AddAdditionalDefines ("MONO_NATIVE_UNIFIED");
				break;
			default:
				throw new Exception ($"Unknown MonoNativeFlavor: {Flavor}");
			}
		}
	}
}
