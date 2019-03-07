/*     INFINITY CODE 2013-2019      */
/*   http://www.infinity-code.com   */

using System;

/// <summary>
/// 
/// </summary>
/// <typeparam name="T"></typeparam>
/// <typeparam name="U"></typeparam>
[Serializable]
public abstract class OnlineMapsMarkerManagerBase<T, U>: OnlineMapsInteractiveElementManager<T, U>, IOnlineMapsSavableComponent
    where T : OnlineMapsMarkerManagerBase<T, U>
    where U: OnlineMapsMarkerBase
{
    protected OnlineMapsSavableItem[] savableItems;

    public static void RemoveItemsByTag(params string[] tags)
    {
        instance.RemoveByTag(tags);
    }

    protected U _CreateItem(double longitude, double latitude)
    {
        U item = Activator.CreateInstance<U>();
        item.SetPosition(longitude, latitude);
        items.Add(item);
        return item;
    }

    public abstract OnlineMapsSavableItem[] GetSavableItems();

    protected override void OnEnable()
    {
        base.OnEnable();

        _instance = (T)this;
    }

    public void RemoveByTag(params string[] tags)
    {
        if (tags.Length == 0) return;

        RemoveAll(m =>
        {
            for (int j = 0; j < tags.Length; j++) if (m.tags.Contains(tags[j])) return true;
            return false;
        });
    }

    protected virtual OnlineMapsJSONItem SaveSettings()
    {
        OnlineMapsJSONArray jitems = new OnlineMapsJSONArray();
        foreach (U marker in items) jitems.Add(marker.ToJSON());
        OnlineMapsJSONObject json = new OnlineMapsJSONObject();
        json.Add("settings", new OnlineMapsJSONObject());
        json.Add("items", jitems);
        return json;
    }

    protected virtual void Start()
    {
        
    }

    protected virtual void Update()
    {
        
    }
}