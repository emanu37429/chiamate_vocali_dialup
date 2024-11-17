using NAudio.Codecs;
using NAudio.Wave;
using System.IO.Ports;

Console.WriteLine("Inserisci numero porta COM");
string port = "COM" + Console.ReadLine();
SerialPort serial = new SerialPort(port);

serial.BaudRate = 230400;
serial.DataBits = 8;
serial.Parity = Parity.None;
serial.StopBits = StopBits.One;
serial.Encoding = System.Text.Encoding.ASCII;
serial.Open();
serial.WriteLine("ATZ\r"); // soft reset

Console.WriteLine("Connesso a " + port);

Console.WriteLine("\nInserisci numero di telefono");
string tel = Console.ReadLine();

serial.DataReceived += Serial_InfoIncoming;
string status = "";
void Serial_InfoIncoming(object sender, SerialDataReceivedEventArgs e)
{
    var msg = serial.ReadExisting().Trim();
    Console.WriteLine(msg);
    if (msg != "")
    {
        status = msg.Contains('\n') ? msg.Split('\n').Last().Trim() : msg;
    }
}

await SendCommand("AT+FCLASS=8", false); //imposta voice mode
await SendCommand("AT+VSM=131,8000", false); //imposta formato audio - per CX93010, 8 bit, 1ch, 8000bps, uLaw 711
await SendCommand("ATDT" + tel, false); //chiama

serial.DataReceived -= Serial_InfoIncoming;

await SendCommand("AT+VTR", true); //inizia comunicazione audio full duplex
await Task.Delay(300);

var stFormat = new WaveFormat(8000, 16, 1);

//inizio invio audio
var voiceIn = new WaveInEvent();
voiceIn.WaveFormat = stFormat;
voiceIn.DataAvailable += VoiceIn_DataAvailable;
void VoiceIn_DataAvailable(object? sender, WaveInEventArgs e)
{
    var encoded = new List<byte>();

    for (int n = 0; n < e.BytesRecorded; n += 2)
    {
        var bt = MuLawEncoder.LinearToMuLawSample(BitConverter.ToInt16(e.Buffer, n));
        if(bt == 0x10)
        {
            encoded.Add(bt); encoded.Add(bt);
        } else encoded.Add(bt);
    }
    var arr = encoded.ToArray();
    serial.Write(arr, 0, arr.Length);
}
voiceIn.StartRecording();

//inizio riproduzione audio ricevuto
var bufferOut = new BufferedWaveProvider(stFormat);
bufferOut.BufferDuration = TimeSpan.FromMilliseconds(200);
bufferOut.DiscardOnBufferOverflow = true;
var voiceOut = new WaveOut();
bool rec = false;
serial.DataReceived += delegate
{
    var toRead = new byte[serial.BytesToRead];
    serial.Read(toRead, 0, toRead.Length);
    var encoded = new List<byte>();
    for(int i=0;i<toRead.Length;i+=2)
    {
        if (toRead[i] == 0x10)
        {
            if (toRead[i+1] == 0x10) encoded.Add(toRead[i]);
        } 
        else { encoded.Add(toRead[i]); encoded.Add(toRead[i + 1]); }
    }

    var decoded = new byte[encoded.Count * 2];
    int outIndex = 0;
    for (int n = 0; n < encoded.Count; n++)
    {
        short decodedSample = MuLawDecoder.MuLawToLinearSample(encoded[n]);
        decoded[outIndex++] = (byte)(decodedSample & 0xFF);
        decoded[outIndex++] = (byte)(decodedSample >> 8);
    }
    bufferOut.AddSamples(decoded, 0, decoded.Length);

    if (!rec)
    {
        rec = true;
        voiceOut.Init(bufferOut);
        voiceOut.Play();
    }
};

Console.WriteLine("\nTrasmissione iniziata...\nPremi invio per uscire.");
Console.ReadKey();

async Task SendCommand(string command, bool end)
{
    status = "";
    await Task.Delay(500);
    Console.WriteLine("\nInvio comando " + command);
    serial.WriteLine(command + "\r");
    while (!end)
    {
        await Task.Delay(500);
        if (status.Contains("K")) { Console.WriteLine("> OK"); break; }
        else if (status.Contains("ERROR")) { Console.WriteLine("> ERROR"); Environment.Exit(5); }
    }
}
