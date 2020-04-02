# Telegram.Bot.Framework

This Framework is an early beta which will be developed the next few weeks / months. It's based on [Telegram.Bot](https://github.com/TelegramBots/Telegram.Bot).

The goal is to achieve a simple and lightweight framework to build chat bots with reusable code.


## ICommand
An ICommand is a simple command without parameters. Each class wich derives from ICommand can be called by different commands through the CommandAttribute.

### Example 1 - No parameters
    [Command("/start")] //The CommandAttribute defines the command to call the Command from Telegram-Chat
    [Command("/start2")] //You can define several commands
    class StartCommand : ICommand
    {
        public async Task<bool> CanInvoke(ITelegramBotClient client, Update update, object args) //CanInvoke gives the ability to check whether the command can be actually called
        {
            return true;
        }

        public async Task<bool> Invoke(ITelegramBotClient client, Update update, object args)
        {
            return await data.Client.SendTextMessageAsync(data.Update.Message.Chat.Id, "Hello World!");
            return true;
        }
    }
### Example 2 - With parameters
    //You can simply define the paramater with {} and define the parameter container
    //If you send the message "/start 5", the bot will answer with "Hello World 5!"
    [Command("/start {id}", ArgumentType = typeof(StartCommandParameter))]
    //The default command can be in the same class
    //If you send the message "/start", the bot will answer with "Hello World!"
    [Command("/start")]
    class StartCommand : ICommand, ICommand<StartCommandParameter>
    {
        public async Task<bool> CanInvoke(ITelegramBotClient client, Update update, object args) //CanInvoke gives the ability to check whether the command can be actually called
        {
            return true;
        }

        public async Task<bool> Invoke(ITelegramBotClient client, Update update, object args)
        {
            await data.Client.SendTextMessageAsync(data.Update.Message.Chat.Id, "Hello World!");
            return true;
        }
        public async Task<bool> CanInvoke(ITelegramBotClient client, Update update, StartCommandParameter args)
        {
            return true;
        }

        public async Task<bool> Invoke(ITelegramBotClient client, Update update, StartCommandParameter args) //If the id were an integer, args.Id is set
        {
            await data.Client.SendTextMessageAsync(data.Update.Message.Chat.Id, "Hello World "+ args.Id +"!");
            return true;
        }
    }
    class StartCommandParameter
    {
        public int Id { get; set; }
    }
    
##Initialize and start using the bot

        static void Main(string[] args)
        {
            var client = new Telegram.Bot.TelegramBotClient("your_token"); //initialize the client
            _helper = new Telegram.Bot.Framework.TelegramBotClientHelper(client); //inititalize the client helper
            _helper.CommandManager.RegisterCommand(new StartCommand()); //register the StartCommand from Example 1 or 2
            client.StartReceiving(); //Start receiving commands

            while (true)
                Console.ReadKey();
        }
