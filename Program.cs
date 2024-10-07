using System;
using System.IO;
using System.Collections.Generic;
using NAudio.Wave;
using System.Speech.Recognition;
using System.Linq;
using System.Speech.AudioFormat;

public class DiscordRecorder
{
    private WaveInEvent waveSource;
    private SpeechRecognitionEngine recognizer;
    private List<TranscriptEntry> transcript = new List<TranscriptEntry>();
    private Dictionary<string, VoiceProfile> voiceProfiles = new Dictionary<string, VoiceProfile>();

    public void StartRecording()
    {
        InitializeVoiceProfiles();
        InitializeRecognizer();
        InitializeAudioCapture();
        Console.WriteLine("Opname gestart. Druk op Enter om te stoppen...");
    }

    private void InitializeVoiceProfiles()
    {
        voiceProfiles.Add("User1", new VoiceProfile("User1", new double[] { 100, 150, 200 }));
        voiceProfiles.Add("User2", new VoiceProfile("User2", new double[] { 120, 170, 220 }));
        voiceProfiles.Add("User3", new VoiceProfile("User3", new double[] { 90, 140, 190 }));
    }

    private void InitializeRecognizer()
    {
        recognizer = new SpeechRecognitionEngine(new System.Globalization.CultureInfo("en-US"));
        recognizer.LoadGrammar(new DictationGrammar());
        recognizer.SpeechRecognized += Recognizer_SpeechRecognized;
        recognizer.SetInputToAudioStream(new MemoryStream(), new SpeechAudioFormatInfo(44100, AudioBitsPerSample.Sixteen, AudioChannel.Mono));
    }

    private void InitializeAudioCapture()
    {
        waveSource = new WaveInEvent();
        waveSource.DataAvailable += (s, e) =>
        {
            recognizer.SetInputToWaveStream(new MemoryStream(e.Buffer));
        };
        waveSource.StartRecording();
        recognizer.RecognizeAsync(RecognizeMode.Multiple);
    }

    private void Recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
    {
        string username = IdentifySpeaker(e.Result.Audio);
        string message = e.Result.Text;
        TranscriptEntry entry = new TranscriptEntry(username, message, DateTime.Now);
        transcript.Add(entry);
        Console.WriteLine(entry);
    }


    private string IdentifySpeaker(RecognizedAudio audio)
    {
        Random rand = new Random();
        double[] features = new double[] { rand.NextDouble() * 100 + 100, rand.NextDouble() * 100 + 100, rand.NextDouble() * 100 + 100 };

        return voiceProfiles
            .OrderBy(vp => CalculateDistance(features, vp.Value.VoiceFeatures))
            .First().Key;
    }

    private double CalculateDistance(double[] features1, double[] features2)
    {
        return Math.Sqrt(features1.Zip(features2, (a, b) => Math.Pow(a - b, 2)).Sum());
    }

    public void StopRecording()
    {
        waveSource.StopRecording();
        recognizer.RecognizeAsyncStop();
        SaveTranscription("discord_transcriptie.txt");
    }

    private void SaveTranscription(string fileName)
    {
        string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), fileName);

        try
        {
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                foreach (var entry in transcript)
                {
                    writer.WriteLine(entry.ToString());
                }
            }
            Console.WriteLine($"Transcriptie opgeslagen in {filePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fout bij het opslaan van de transcriptie: {ex.Message}");
        }
    }
}

public class TranscriptEntry
{
    public string Username { get; }
    public string Message { get; }
    public DateTime Timestamp { get; }

    public TranscriptEntry(string username, string message, DateTime timestamp)
    {
        Username = username;
        Message = message;
        Timestamp = timestamp;
    }

    public override string ToString()
    {
        return $"[{Timestamp}] {Username}: {Message}";
    }
}

public class VoiceProfile
{
    public string Username { get; }
    public double[] VoiceFeatures { get; }

    public VoiceProfile(string username, double[] voiceFeatures)
    {
        Username = username;
        VoiceFeatures = voiceFeatures;
    }
}

class Program
{
    static void Main(string[] args)
    {
        DiscordRecorder recorder = new DiscordRecorder();

        Console.WriteLine("Druk op Enter om de opname te starten...");
        Console.ReadLine();

        recorder.StartRecording();
        Console.ReadLine(); // Wacht op Enter om te stoppen

        recorder.StopRecording();
    }
}
