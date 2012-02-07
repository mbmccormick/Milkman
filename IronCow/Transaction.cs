using IronCow.Rest;

namespace IronCow
{
    public class Transaction
    {
        public int Id { get; private set; }
        public bool Undoable { get; private set; }
        public string Comment { get; internal set; }

        internal Transaction(RawTransaction transaction)
        {
            Id = transaction.Id.GetValueOrDefault(-1);
            Undoable = (transaction.Undoable == 1);
        }
    }
}
