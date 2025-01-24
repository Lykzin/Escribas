using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace DiscordRoleBot
{
    class Program
    {
        static void Main(string[] args) => new Program().RunBotAsync().GetAwaiter().GetResult();

        private DiscordSocketClient _client;
        private IServiceProvider _services;

        public async Task RunBotAsync()
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers,
                LogLevel = LogSeverity.Info
            });

            _services = new ServiceCollection()
                .AddSingleton(_client)
                .BuildServiceProvider();

            _client.Log += LogAsync;
            _client.Ready += OnReadyAsync;
            _client.InteractionCreated += HandleInteractionAsync;

            string botToken = ""; // Substitua pelo seu token do bot
            await _client.LoginAsync(TokenType.Bot, botToken);
            await _client.StartAsync();

            await Task.Delay(-1);
        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }

        private async Task OnReadyAsync()
        {
            Console.WriteLine("Bot está online!");

            var guild = _client.GetGuild(1302269610422304769); // ID do servidor
            var channel = guild.GetTextChannel(1304115518294462475); // ID do canal onde o botão será enviado

            var builder = new ComponentBuilder()
                .WithButton("Receber Cargo", "assign_role", ButtonStyle.Success);

            // A mensagem completa
            string fullMessage = "\U0001f9ff Bem-vindo ao Acampamento Meio-Sangue! \U0001f9ff\r\n\r\nSaudações, semideuses! Se você está aqui, é porque o Olimpo decidiu que você tem algo de especial. Seja bem-vindo ao nosso RPG inspirado no universo de Percy Jackson, onde cada um de vocês é um semideus em treinamento no Acampamento Meio-Sangue!\r\n\r\n⚔️ **Como funciona o RPG?**\r\n\r\nEste é um RPG de mesa onde cada sessão será uma aventura única — uma one-shot. Em cada uma dessas aventuras, vocês serão designados para missões diferentes, onde terão a chance de testar suas habilidades, enfrentar monstros mitológicos e resolver mistérios. Cada missão é autônoma, então você não precisa participar de todas.\r\n\r\nAs missões vão estar no canal <#1304116880381968445>. Teremos mais de 3 missões por mês. Para se inscrever em uma missão, basta reagir à mensagem correspondente. Cada missão terá um limite e um mínimo de participantes necessários para ser iniciada (essas informações estarão descritas na própria missão). As missões possuem dificuldades que variam de Rank E até Rank SS. Quanto maior a dificuldade, maiores serão as recompensas — e, claro, os desafios!\r\n\r\nCada semideus vai poder participar de duas missão por mês! então escolha bem sua missão.\r\n\r\n🎁 **Recompensas e Progressão**\r\n\r\nCada missão completada traz uma série de recompensas: itens mágicos, bênçãos dos deuses, habilidades especiais ou até relíquias únicas. Essas recompensas não só ajudam você nas próximas aventuras, mas também representam seu progresso e status dentro do acampamento!\r\n\r\nQuanto mais missões o semideus completar, mais qualificado ele será pra participar de missões mais dificéis, e mais forte ele irá se tornar. \r\n📚 **Criação de Personagem**\r\n\r\nCada jogador cria um semideus com uma história e habilidades únicas. Para começar, consulte o guia de criação de personagens no canal ⁠<#1304118879517147247> .\r\n\r\n(SEU PAI OU MÃE DIVINO SERÁ SORTEADO!!!)\r\n\r\nNão se preocupe com detalhes técnicos da ficha — o <@&1302274092401168448> irá te enviar a ficha. Concentre-se em desenvolver a história e a personalidade do seu personagem! \r\n\r\n🏕️ **O Acampamento Meio-Sangue**\r\n\r\nO Acampamento Meio-Sangue é o refúgio seguro para os semideuses, longe da ameaça constante dos monstros que caçam os filhos dos deuses. Localizado em uma área isolada, este é um lugar onde semideuses de todas as origens podem treinar, aprender e se preparar para enfrentar os desafios do mundo mitológico.\r\n\r\nNo acampamento é aonde vocês passarão a maior parte do seu tempo enquanto não estiverem em uma missão, lá vocês poderão treinar, forjar armas, testar diferentes armas e entre outras coisas. Além disso, vocês terão a oportunidade de aprender com outros semideuses, praticar a arte da caça, e até mesmo se especializar em diferentes técnicas de combate, magia e sobrevivência.";

            // Dividindo a mensagem em duas partes
            string part1 = fullMessage.Length > 2000 ? fullMessage.Substring(0, 2000) : fullMessage;
            string part2 = fullMessage.Length > 2000 ? fullMessage.Substring(2000) : "";

            // Envia a primeira parte da mensagem sem o botão
            await channel.SendMessageAsync(part1);

            // Se houver uma segunda parte, envia ela com o botão
            if (!string.IsNullOrEmpty(part2))
            {
                await channel.SendMessageAsync(part2, components: builder.Build());
            }
            else
            {
                // Caso não haja segunda parte, adiciona o botão na primeira mensagem
                await channel.SendMessageAsync("Caso queira se inscrever, clique no botão abaixo:", components: builder.Build());
            }
        }

        private async Task HandleInteractionAsync(SocketInteraction interaction)
        {
            if (interaction is SocketMessageComponent component && component.Data.CustomId == "assign_role")
            {
                var guildUser = (SocketGuildUser)component.User;

                // ID do cargo que será atribuído
                var role = guildUser.Guild.GetRole(1302273956283416710);

                if (!guildUser.Roles.Contains(role))
                {
                    await guildUser.AddRoleAsync(role);
                    await component.RespondAsync($"{guildUser.Mention}, você recebeu o cargo **{role.Name}**!", ephemeral: true);

                    // Chama a função para criar categoria e canais para o usuário
                    await CreateCategoryAndChannels(guildUser);
                }
                else
                {
                    await component.RespondAsync("Você já possui este cargo!", ephemeral: true);
                }
            }
        }

        private async Task CreateCategoryAndChannels(SocketGuildUser guildUser)
        {
            var guild = guildUser.Guild;  // Obtém o servidor
            var categoryName = $"chalé de {guildUser.Username}";  // Nome da categoria com base no nome de usuário
            var category = await guild.CreateCategoryChannelAsync(categoryName);  // Cria a categoria

            // Criar  canais de texto dentro dessa categoria
            var channel4 = await guild.CreateTextChannelAsync("Deuses", prop => prop.CategoryId = category.Id);
            var channel1 = await guild.CreateTextChannelAsync("anotações", prop => prop.CategoryId = category.Id);
            var channel2 = await guild.CreateTextChannelAsync("lore", prop => prop.CategoryId = category.Id);
            var channel3 = await guild.CreateTextChannelAsync("ficha", prop => prop.CategoryId = category.Id);


            // Definindo permissões para todos: Negar visualização
            var denyPermissions = new OverwritePermissions(
                viewChannel: PermValue.Deny,
                sendMessages: PermValue.Deny
            );

            // Aplica as permissões negadas para todos na categoria
            await category.AddPermissionOverwriteAsync(guild.EveryoneRole, denyPermissions);

            // Definindo permissões para o usuário: Permitir visualização e envio de mensagens
            var allowPermissions = new OverwritePermissions(
                viewChannel: PermValue.Allow,
                sendMessages: PermValue.Allow
            );

            // Permitir que o usuário acesse os canais
            await channel1.AddPermissionOverwriteAsync(guildUser, allowPermissions);
            await channel2.AddPermissionOverwriteAsync(guildUser, allowPermissions);
            await channel3.AddPermissionOverwriteAsync(guildUser, allowPermissions);
            await channel4.AddPermissionOverwriteAsync(guildUser, allowPermissions);

            // Informando que a categoria e os canais foram criados com sucesso
            await guild.DefaultChannel.SendMessageAsync($"Categoria e canais criados para {guildUser.Username} com sucesso!");
        }
    }
}
