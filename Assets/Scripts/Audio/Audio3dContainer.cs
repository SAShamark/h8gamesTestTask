namespace Audio
{
    public class Audio3dContainer : AudioContainer
    {
        protected override AudioSourceModel Create()
        {
            AudioSourceModel audioSourceModel = base.Create();
            audioSourceModel.Source.spatialBlend = 1;

            return audioSourceModel;
        }
    }
}