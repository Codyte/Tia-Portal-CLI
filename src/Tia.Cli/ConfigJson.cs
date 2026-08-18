using System;
using System.IO;
using Newtonsoft.Json;

namespace Tia.Cli
{
    /// <summary>
    /// Leitura dos arquivos de config dos geradores (`--config`).
    ///
    /// O default do Newtonsoft ignora propriedade desconhecida, então um typo em `TemplateFolder`
    /// (ou uma chave de outra versão do config) sumia em silêncio e o gerador rodava no default
    /// amplo — o mesmo estrago de uma opção de linha de comando ignorada, pela outra porta.
    /// Aqui propriedade desconhecida é erro de uso.
    ///
    /// Exceção deliberada: chave iniciada por `_` é comentário (JSON não tem sintaxe de comentário,
    /// e os exemplos do repo usam `_comment`).
    /// </summary>
    public static class ConfigJson
    {
        public static JsonSerializerSettings Settings
        {
            get
            {
                var settings = new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Error,
                };
                settings.Error = (sender, args) =>
                {
                    var member = args.ErrorContext.Member as string;
                    if (member != null && member.StartsWith("_", StringComparison.Ordinal))
                        args.ErrorContext.Handled = true;
                };
                return settings;
            }
        }

        public static T Read<T>(string path)
        {
            return JsonConvert.DeserializeObject<T>(File.ReadAllText(path), Settings);
        }

        public static object Read(string path, Type type)
        {
            return JsonConvert.DeserializeObject(File.ReadAllText(path), type, Settings);
        }
    }
}
