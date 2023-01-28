namespace SamSharp.Renderer
{
    public class Options
    {
        public byte Pitch { get; }
        public byte Mouth { get; }
        public byte Throat { get; }
        public byte Speed { get; }
        public bool SingMode { get; }

        public Options(byte pitch = 64, byte mouth = 128, byte throat = 128, byte speed = 72, bool singMode = false)
        {
            Pitch = pitch;
            Mouth = mouth;
            Throat = throat;
            Speed = speed;
            SingMode = singMode;
        }
    }
}