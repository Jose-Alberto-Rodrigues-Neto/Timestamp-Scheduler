public class DadosObjeto
{
    public string Nome { get; private set; }
    public int TS_Read { get; set; } = 0;
    public int TS_Write { get; set; } = 0;
    public string LogPath { get; set; } = "";

    public DadosObjeto(string nome)
    {
        Nome = nome;
        LogPath = $"logs/log_{Nome}.txt";
    }

    public void SalvarLog(string escalonamento, string operacao, int momento)
    {
        File.AppendAllText(LogPath, $"{escalonamento},{operacao},{momento}\n");
    }
}
