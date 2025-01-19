using NAudio.Wave;

class AudioRecorder
{
    private WaveInEvent _waveIn;
    private MemoryStream _memoryStream;
    private const int SampleRate = 16000;
    private const int Channels = 1;
    private const int BitsPerSample = 16;

    public AudioRecorder()
    {
        _waveIn = new WaveInEvent();
        _waveIn.DeviceNumber = 0;
        _waveIn.WaveFormat = new WaveFormat(SampleRate, BitsPerSample, Channels);
        _waveIn.BufferMilliseconds = 100;
        _waveIn.DataAvailable += OnDataAvailable;
        _memoryStream = new MemoryStream();
    }

    private void OnDataAvailable(object sender, WaveInEventArgs e)
    {
        _memoryStream.Write(e.Buffer, 0, e.BytesRecorded);
    }

    public async Task<byte[]> RecordAudioAsync(int seconds)
    {
        _memoryStream.SetLength(0);
        _waveIn.StartRecording();

        await Task.Delay(seconds * 1000);

        _waveIn.StopRecording();
        return _memoryStream.ToArray();
    }
}
