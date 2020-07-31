using System;

namespace tainicom.Aether.Content.Pipeline.Processors
{
	public enum WpfTextureFormat
	{
		Color,
		Bgra4444,
		Auto       // 自動:単色の場合にはDXT3、アウトライン使用でBgra444、
				   // グラデーション使用でColorとフォーマットを切り替える
	}
}
