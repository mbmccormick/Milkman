function Locations()
{
    var RtmApiKey = "09b03090fc9303804aedd945872fdefc";
    var RtmSharedKey = "d2ffaf49356b07f9";

    var registrationsTable = tables.getTable('Registrations');
    registrationsTable.read({
        success: function(registrations)
        {
            registrations.forEach(function(registration)
            {
                processRegistration(registration);
            });
        }
    });
    
    function processRegistration(registration)
    {
        getLocations(registration);
    }

    function getLocations(registration)
    {
        var httpRequest = require('request');
                    
        var url = "https://api.rememberthemilk.com/services/rest/?method=rtm.locations.getList&api_key=" + RtmApiKey + 

"&format=json&auth_token=" + registration.authenticationToken; 
        
        var params = RtmSharedKey + "api_key" + RtmApiKey + "auth_token" + registration.authenticationToken + "format" + 

"json" + "method" + "rtm.locations.getList";
    
        var crypto = require('crypto');
        var signature = crypto.createHash('md5').update(params).digest('hex');
    
        url = url + "&api_sig=" + signature;
        
        httpRequest.get({
            url: url
        },
        function(err, response, body)
        {
            if (err)
            {
                console.error("Unable to connect to RTM:", response);
            }
            else if (response.statusCode != 200)
            {
                console.error("Error communicating with RTM:", response);
            }
            else
            {
                // console.log("Request succeeded:", body);
                
                var data = JSON.parse(body);
                var locations = data.rsp.locations.location;
    
                if (locations !== undefined)
                {
                    try
                    {
                        locations.forEach(function (item)
                        {
                            var nearbyInterval = registration.nearbyInterval;
                                
                            if (distance(registration.latitude, registration.longitude, item.latitude, item.longitude) <= nearbyInterval)
                            {
                                getTasks(registration, item.id);
                            }
                        });
                    }
                    catch (ex)
                    {
                        var nearbyInterval = registration.nearbyInterval;
                            
                        if (distance(registration.latitude, registration.longitude, locations.latitude, locations.longitude) <= nearbyInterval)
                        {
                            getTasks(registration, locations.name);
                        }
                    }
                }
            }
        });
    }

    function getTasks(registration, location)
    {
        var httpRequest = require('request');
                    
        var url = "https://api.rememberthemilk.com/services/rest/?method=rtm.tasks.getList&api_key=" + RtmApiKey + "&format=json&auth_token=" + registration.authenticationToken + "&filter=status:incomplete AND location:" + location; 
        
        var params = RtmSharedKey + "api_key" + RtmApiKey + "auth_token" + registration.authenticationToken + "filter" + "status:incomplete AND location:" + location + "format" + "json" + "method" + "rtm.tasks.getList";
    
        var crypto = require('crypto');
        var signature = crypto.createHash('md5').update(params).digest('hex');
    
        url = url + "&api_sig=" + signature;
        
        httpRequest.get({
            url: url
        },
        function(err, response, body)
        {
            if (err)
            {
                console.error("Unable to connect to RTM:", response);
            }
            else if (response.statusCode != 200)
            {
                console.error("Error communicating with RTM:", response);
            }
            else
            {
                // console.log("Request succeeded:", body);
                
                var data = JSON.parse(body);
                var lists = data.rsp.tasks.list;
    
                if (lists !== undefined)
                {
                    lists.forEach(function (item1)
                    {
                        var tasks = item1.taskseries;
                        
                        try
                        {
                            tasks.forEach(function (item2)
                            {
                                sendToastNotification(registration, item2.name,  "This task is nearby.");
                            });
                        }
                        catch (ex)
                        {
                            sendToastNotification(registration, tasks.name,  "This task is nearby.");
                        }
                    });
                }
            }
        });
    }
    
    function distance(lat1, lon1, lat2, lon2)
    {
        var radlat1 = Math.PI * lat1 / 180;
        var radlat2 = Math.PI * lat2 / 180;
        var radlon1 = Math.PI * lon1 / 180;
        var radlon2 = Math.PI * lon2 / 180;

        var theta = lon1 - lon2;
        var radtheta = Math.PI * theta / 180;

        var dist = Math.sin(radlat1) * Math.sin(radlat2) + Math.cos(radlat1) * Math.cos(radlat2) * Math.cos(radtheta);

        dist = Math.acos(dist);
        dist = dist * 180/Math.PI;
        dist = dist * 60 * 1.1515;

        return dist;
    }
    
    function sendToastNotification(registration, text1, text2)
    {
        push.mpns.sendToast(registration.handle, {
            text1: text1,
            text2: text2
        },
        {
            success: function(pushResponse)
            {
                console.log("Sent push:", pushResponse);
            }
        });
    }
}