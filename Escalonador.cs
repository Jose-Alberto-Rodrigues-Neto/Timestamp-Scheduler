using System;
using System.Collections.Generic;
using System.IO;

public class Escalonador
{
    private readonly string inputPath;
    private readonly string outputPath;
    private List<string> objetos = new();
    private Dictionary<string, int> timestamps = new();
    private string[] linhasEscalonamentos = Array.Empty<string>();

    public Escalonador(string inputPath, string outputPath)
    {
        this.inputPath = inputPath;
        this.outputPath = outputPath;
        Directory.CreateDirectory("logs");
    }

    public void Executar()
    {
        var linhas = File.ReadAllLines(inputPath);
        objetos.AddRange(linhas[0].TrimEnd(';').Split(", "));

        var transacoes = linhas[1].TrimEnd(';').Split(", ");
        var ts = linhas[2].TrimEnd(';').Split(", ");
        for (int i = 0; i < transacoes.Length; i++)
            timestamps[transacoes[i]] = int.Parse(ts[i]);

        linhasEscalonamentos = linhas[3..];

        using StreamWriter outWriter = new StreamWriter(outputPath);

        foreach (var linha in linhasEscalonamentos)
        {
            string resultado = ProcessarEscalonamento(linha.Trim());
            outWriter.WriteLine(resultado);
        }
    }

    private string ProcessarEscalonamento(string linha)
    {
        string idEscalonamento = linha.Split('-')[0];
        string[] operacoes = linha[(idEscalonamento.Length + 1)..].Trim().Split(' ');

        var estadoObjetos = new Dictionary<string, DadosObjeto>();
        foreach (var nome in objetos)
        {
            var dado = new DadosObjeto(nome);
            estadoObjetos[nome] = dado;
            File.WriteAllText(dado.LogPath, "");
        }

        var objetosUsadosPorTransacao = new Dictionary<string, HashSet<string>>();
        var transacaoPorDado = new Dictionary<string, string>();
        int momento = 0;

        foreach (string op in operacoes)
        {
            if (string.IsNullOrWhiteSpace(op)) continue;

            if (op.StartsWith("c"))
            {
                foreach (var dado in estadoObjetos.Values)
                {
                    dado.TS_Read = 0;
                    dado.TS_Write = 0;
                }
                continue;
            }

            if (op.Length < 5 || op[2] != '(' || !op.EndsWith(")"))
                continue;

            char tipo = op[0];
            string tid = "t" + op[1];
            string obj = op[3].ToString();
            int tsT = timestamps.ContainsKey(tid) ? timestamps[tid] : 0;

            if (!objetosUsadosPorTransacao.ContainsKey(tid))
                objetosUsadosPorTransacao[tid] = new HashSet<string>();

            objetosUsadosPorTransacao[tid].Add(obj);
            transacaoPorDado[obj] = tid;

            momento++;

            bool permitido = true;

            if (tipo == 'r')
            {
                permitido = tsT >= estadoObjetos[obj].TS_Write;
                if (!permitido)
                    return $"{idEscalonamento}-ROLLBACK-{momento}";

                if (tsT > estadoObjetos[obj].TS_Read)
                    estadoObjetos[obj].TS_Read = tsT;
            }
            else if (tipo == 'w')
            {
                permitido = tsT >= estadoObjetos[obj].TS_Read;
                if (!permitido)
                    return $"{idEscalonamento}-ROLLBACK-{momento}";

                if (tsT >= estadoObjetos[obj].TS_Write)
                    estadoObjetos[obj].TS_Write = tsT;
            }
            estadoObjetos[obj].SalvarLog(idEscalonamento, tipo == 'r' ? "Read" : "Write", momento);            
        }

        return $"{idEscalonamento}-OK";
    }
}