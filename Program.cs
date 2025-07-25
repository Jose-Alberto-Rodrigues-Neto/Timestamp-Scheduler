Directory.CreateDirectory("logs");
string pasta = "logs";
foreach (string arquivo in Directory.GetFiles(pasta))
{
    File.Delete(arquivo);
}
string inputPath = "in.txt";
string outputPath = "out.txt";        
Escalonador escalonador = new Escalonador(inputPath, outputPath);
escalonador.Executar();