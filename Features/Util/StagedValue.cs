
namespace KatieSaveHelper.Features.Util
{
    public class StagedValue<T>
    {
        private bool ready;
        private T value;

        public bool IsReady => ready;

        public StagedValue()
        {
            ready = false;
            value = default(T);
        }

        public T TakeValue()
        {
            if (!this.ready)
                throw new System.Exception("Value is not marked as ready.");

            this.ready = false;
            T val = this.value;
            this.value = default(T);
            return val;
        }

        public T PeekValue()
        {
            return this.value;
        }

        public void StageValue(T value)
        {
            this.ready = true;
            this.value = value;
        }

        public void ResetStage()
        {
            this.ready = false;
            this.value = default(T);
        }

        public void ForceReady()
        {
            this.ready = true;
        }

        public void ForceNotReady()
        {
            this.ready = false;
        }
    }
}