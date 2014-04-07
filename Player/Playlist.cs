namespace DriftPlayer
{
    //[Serializable()]
    //class Playlist<T> : List<T>, ISerializable
    //{
    //    int index;

    //    public Playlist()
    //    {
    //        this.index = 0;
    //    }

    //    public T Next()
    //    {
    //        index++;
    //        if (index >= this.Count)
    //            index = 0;
    //        if (this.Count == 0)
    //            return default(T);
    //        else
    //            return this[index];
    //    }

    //    public T Prev()
    //    {
    //        index--;
    //        if (index <= -1)
    //            index = this.Count - 1;
    //        if (this.Count == 0)
    //            return default(T);
    //        else
    //            return this[index];
    //    }

    //    public void Select(T item)
    //    {
    //        this.index = this.IndexOf(item);
    //    }

    //    public Playlist(SerializationInfo info, StreamingContext context)
    //    {
    //        this.index = (int)info.GetValue("index", typeof(int));
    //    }

    //    public void GetObjectData(SerializationInfo info, StreamingContext context)
    //    {
    //        info.AddValue("index", this.index);
    //    }
    //}
}
