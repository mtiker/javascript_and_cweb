namespace App.DTO.v1;

public class Message
{
    public Message()
    {
    }

    public Message(string message)
    {
        Messages = [message];
    }

    public Message(params string[] messages)
    {
        Messages = messages;
    }

    public string[] Messages { get; set; } = [];
}
