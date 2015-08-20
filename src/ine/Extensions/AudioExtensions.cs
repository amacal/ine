using ine.Domain;
using NAudio.Wave;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ine.Extensions
{
    public static class AudioExtensions
    {
        public static Task Play(this Captcha captcha)
        {
            return Task.Run(() =>
            {
                using (MemoryStream memory = new MemoryStream(captcha.Data, false))
                {
                    memory.Seek(0, SeekOrigin.Begin);

                    using (Mp3FileReader reader = new Mp3FileReader(memory))
                    using (WaveStream pcm = WaveFormatConversionStream.CreatePcmStream(reader))
                    using (WaveStream stream = new BlockAlignReductionStream(pcm))
                    {
                        using (WaveOut waveOut = new WaveOut(WaveCallbackInfo.FunctionCallback()))
                        {
                            waveOut.Init(stream);
                            waveOut.Play();

                            while (waveOut.PlaybackState == PlaybackState.Playing)
                            {
                                Thread.Sleep(100);
                            }
                        }
                    }
                }
            });

        }
    }
}
