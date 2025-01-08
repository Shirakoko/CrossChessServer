using System.Text;

/// <summary>
/// 进入大厅
/// </summary>
public class EnterHall : BaseMessage
{
    public string userName; // 大厅里的用户名

    public EnterHall()
    {

    }

    public EnterHall(string userName)
    {
        this.userName = userName;
    }

    public override byte[] ConvertToByteArray()
    {
        int index = 0;
        byte[] bytes = new byte[sizeof(int) + GetBytesNum()];

        WriteInt(bytes, (int)GetMessageID(), ref index);
        WriteString(bytes, userName, ref index);
    
        return bytes;
    }

    public override int GetBytesNum()
    {
        return sizeof(int)+Encoding.UTF8.GetBytes(userName).Length; // 用户名
    }

    public override MessageID GetMessageID()
    {
        return MessageID.EnterHall;
    }

    public override int ReadFromBytes(byte[] bytes, int beginIndex = 0)
    {
        int index = beginIndex;

        userName = ReadString(bytes, ref index);
        return index - beginIndex;
    }
}

/// <summary>
/// 准许进入大厅
/// </summary>
public class AllowEnterHall : BaseMessage
{
    public override byte[] ConvertToByteArray()
    {
        int index = 0;
        byte[] bytes = new byte[sizeof(int) + GetBytesNum()];

        WriteInt(bytes, (int)GetMessageID(), ref index);
        return bytes;
    }

    public override int GetBytesNum()
    {
        return 0;
    }

    public override MessageID GetMessageID()
    {
        return MessageID.AllowEnterHall;
    }

    public override int ReadFromBytes(byte[] bytes, int beginIndex = 0)
    {
        return 0;
    }
}