namespace CSMOO.Core;

public class StringBuilder
{

    private readonly System.Text.StringBuilder _inner;
    public int Length => _inner.Length;

    public StringBuilder()
    {
        _inner = new System.Text.StringBuilder();
    }

    public StringBuilder(int capacity)
    {
        _inner = new System.Text.StringBuilder(capacity);
    }

    public StringBuilder(string value)
    {
        _inner = new System.Text.StringBuilder(value);
    }

    public StringBuilder(string value, int capacity)
    {
        _inner = new System.Text.StringBuilder(value, capacity);
    }

    public void EnsureCapacity(int capacity)
    {
        _inner.EnsureCapacity(capacity);
    }

    public StringBuilder Append(string value)
    {
        _inner.Append(value);
        return this;
    }

    public StringBuilder AppendLine(string value)
    {
        _inner.AppendLine(value);
        return this;
    }

    public StringBuilder AppendLine()
    {
        _inner.AppendLine();
        return this;
    }

    public StringBuilder Clear()
    {
        _inner.Clear();
        return this;
    }

    public override string ToString() => _inner.ToString();
}


