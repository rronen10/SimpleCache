namespace SimpleCache
{
    public interface IKeyAbstruction<TKey>
    {
        public TKey Key { get; }
    }
}
