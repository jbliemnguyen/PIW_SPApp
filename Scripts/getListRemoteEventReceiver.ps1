cls
$site = Get-SPSite -Identity "https://fdc1s-sp23wfed2.ferc.gov/piw"
$web = $site.RootWeb
$list = $web.Lists["PIW Documents"]
# $list

#$list.EventReceivers | Select ReceiverName,ReceiverClass,ReceiverAssembly, EventType, ReceiverURL,Synchronization
$list.EventReceivers