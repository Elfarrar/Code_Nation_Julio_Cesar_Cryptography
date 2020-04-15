using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Codenation.Criptografia_Julio_Cesar
{
    class Program
    {
        const string TOKEN = "caafede7c68d85fcbdede58e2490c598a065000f";
        const string FILERECEIVED = "answer.json";
        private static Uri baseUri = new Uri("https://api.codenation.dev/v1/challenge/dev-ps/");

        static async Task Main(string[] args)
        {
            if (!File.Exists(FILERECEIVED))
                await buscarDado();

            var mensagem = lerArquivo();

            decifraMensagem(mensagem);

            criarCriptografia(mensagem);

            salvarArquivo(mensagem, false);

            await enviaArquivo();

            Console.ReadKey();
        }

        private static async Task buscarDado()
        {
            using (var client = new HttpClient())
            {
                var url = $"{baseUri}generate-data?token={TOKEN}";

                var jsonString = await client.GetStringAsync(url);
                var mensagem = JsonConvert.DeserializeObject<Mensagem>(jsonString);
                salvarArquivo(mensagem);
            }
        }

        private static void salvarArquivo(Mensagem mensagem, bool firstTime = true)
        {
            using (StreamWriter file = File.CreateText(FILERECEIVED))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, mensagem);
            }
        }

        private static Mensagem lerArquivo()
        {
            string jsonString = File.ReadAllText(FILERECEIVED);
            return JsonConvert.DeserializeObject<Mensagem>(jsonString);
        }

        private static void decifraMensagem(Mensagem mensagem)
        {
            mensagem.decifrado = string.Empty;
            foreach (char letraCifrada in mensagem.cifrado.ToLower())
            {
                mensagem.decifrado += Char.IsLetter(letraCifrada) ?
                    (char)((int)letraCifrada - mensagem.numero_casas) :
                    letraCifrada;
            }
        }

        private static void criarCriptografia(Mensagem mensagem)
        {
            byte[] data = UTF8Encoding.UTF8.GetBytes(mensagem.decifrado);
            byte[] result;

            SHA1 sha = new SHA1CryptoServiceProvider();
            result = sha.ComputeHash(data);

            mensagem.resumo_criptografico = BitConverter.ToString(result).Replace("-", "").ToLower();
        }

        private static async Task enviaArquivo()
        {
            var url = $"{baseUri}submit-solution?token={TOKEN}";

            var sr = new StreamReader("answer.json");
            using var memstream = new MemoryStream();
            sr.BaseStream.CopyTo(memstream);
            var fileUpload = memstream.ToArray();

            HttpContent content = new ByteArrayContent(fileUpload);
            using var client = new HttpClient();
            using var formData = new MultipartFormDataContent();

            formData.Add(content, "answer", "answer");

            var response = await client.PostAsync(url, formData);
            if (response.IsSuccessStatusCode)
            {
                var dados = await response.Content.ReadAsStreamAsync();
                Console.WriteLine("Retorno do POST: " + new StreamReader(dados).ReadToEnd());
            }
        }
    }
}
