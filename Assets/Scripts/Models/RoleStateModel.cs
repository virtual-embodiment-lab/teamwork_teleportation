using Normal.Realtime;
using Normal.Realtime.Serialization;

[RealtimeModel]
public partial class RoleStateModel
{
    [RealtimeProperty(1, true, true)]
    private bool _selectNone;

    [RealtimeProperty(2, true, true)]
    private bool _selectCollector;

    [RealtimeProperty(3, true, true)]
    private bool _selectTactical;

    [RealtimeProperty(4, true, true)]
    private bool _selectExplorer;

    [RealtimeProperty(5, true, true)]
    private RealtimeSet<PlayerModel> _playerModels;
   
}