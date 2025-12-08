namespace eSyncMate.Processor.Models
{
    public class MichealOrderConfirmationInputModel
    {
        public List<string>[] orderNumbers { get; set; }

        public MichealOrderConfirmationInputModel()
        {
            this.orderNumbers = new List<string>[0];
        }
    }
}
