using System.CodeDom.Compiler;
using System.Configuration;
using System.Runtime.CompilerServices;

namespace loramesh.Properties
{
	internal sealed class Settings : ApplicationSettingsBase
	{
		private static readonly Settings defaultInstance = (Settings)SettingsBase.Synchronized((SettingsBase)new Settings());

		public static Settings Default
		{
			get
			{
				Settings defaultInstance = Settings.defaultInstance;
				return defaultInstance;
			}
		}
	}
}
