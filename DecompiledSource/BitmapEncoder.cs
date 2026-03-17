using System.IO;

internal class BitmapEncoder
{
	public static void WriteBitmap(Stream stream, int width, int height, byte[] imageData)
	{
		using BinaryWriter binaryWriter = new BinaryWriter(stream);
		binaryWriter.Write((ushort)19778);
		binaryWriter.Write((uint)(54 + width * height * 4));
		binaryWriter.Write((ushort)0);
		binaryWriter.Write((ushort)0);
		binaryWriter.Write(54u);
		binaryWriter.Write(40u);
		binaryWriter.Write(width);
		binaryWriter.Write(height);
		binaryWriter.Write((ushort)1);
		binaryWriter.Write((ushort)32);
		binaryWriter.Write(0u);
		binaryWriter.Write((uint)(width * height * 4));
		binaryWriter.Write(0);
		binaryWriter.Write(0);
		binaryWriter.Write(0u);
		binaryWriter.Write(0u);
		for (int i = 0; i < imageData.Length; i += 3)
		{
			binaryWriter.Write(imageData[i + 2]);
			binaryWriter.Write(imageData[i + 1]);
			binaryWriter.Write(imageData[i]);
			binaryWriter.Write(byte.MaxValue);
		}
	}
}
