namespace GhettosFirearmSDKv2
{
    public class SaveNodeValueItem : SaveNodeValue
    {
        private ItemSaveData _value;
        public ItemSaveData Value
        {
            get
            {
                if (_value == null)
                    _value = new ItemSaveData();
                return _value;
            }
            set
            {
                _value = value;
            }
        }
    }
}
