
const edge = require('edge-js');
const async = require('async');
const readline = require('readline');

let plcConns = new Map();



var Connect_ = edge.func({
    assemblyFile: '.\\S7CommPlusDllWrapper\\bin\\x64\\Debug\\S7CommPlusDllWrapper.dll', 
    typeName: 'S7CommPlusDriverWrapper.DriverManager',
    methodName: 'Connect'
});
const Connect = (ipAddress, password, timeout) => {
    return new Promise((resolve, reject) => {

        // init input object
        input = {
            ipAddress: ipAddress,
            password: password,
            timeout: timeout
        }
        Connect_(input, (error, output) => {
            if (error) {
                reject(error);
                return;
            } // else
            // connect executed succesfully

            // parse output object
            let connRes = output.Item1;
            let sessID2 = output.Item2;

            if (connRes != 0) {
                // connection unsuccessful
                reject(new Error(
                    "unable to connect to PLC @ IP: " + ipAddress + " (connection code: " + connRes + ")"
                ));
                return;
            } // else
            // connection successful 

            // track PLC connection 
            plcConns.set(ipAddress, sessID2);
            resolve({
                ipAddress: ipAddress, 
                sessID2: sessID2
            });
        });
    });
};
const MultiConnect = (connParamArray) => {
    return new Promise((resolve, reject) => {

        let connResults = [];
        async.each(connParamArray, async function (connParam, callback) {
            try {
                // attempt connection
                const connRes = await Connect(
                    connParam.ipAddress,
                    connParam.password,
                    connParam.timeout
                )
                // store successful connection result
                connResults.push({
                    status: "Success",
                    IP: connRes.ipAddresss,
                    SessID: connRes.sessID2
                });
                // proceed to next connection attempt
                callback();
            } catch (error) {
                // store unsuccessful connection result
                connResults.push({
                    status: "Failure",
                    IP: connParam.ipAddress,
                    SessID: 0,
                    error: error
                });
                // proceed to next connection attempt
                callback();
            }
        }, function (err) {
            if (err) {
                reject(new Error(
                    "An Error has occured while attempting to connect to PLCs"
                ));
                return;
            } 
            resolve(connResults);
        });
    });
}



var Disconnect_ = edge.func({
    assemblyFile: '.\\S7CommPlusDllWrapper\\bin\\x64\\Debug\\S7CommPlusDllWrapper.dll', 
    typeName: 'S7CommPlusDriverWrapper.DriverManager',
    methodName: 'Disconnect'
});
const Disconnect = (ipAddress) => {
    return new Promise((resolve, reject) => {

        // check for IP
        if ( !(plcConns.has(ipAddress)) ) {
            // attempting to disconnect from a connection that does not exists technically counts as a successful disconnect
            console.log("Alert:\n >>>No such PLC connection @ IP: " + ipAddress + " exists");
            resolve(1);
            return;
        } // else
        // retrieve corresponding sessionID for IP
        let sessionID2 = plcConns.get(ipAddress);

        // init input object
        input = {
            sessionID2: sessionID2
        }
        Disconnect_(input, (error, output) => {
            if (error) {
                reject(error);
                return;
            } // else
            // Disconnect executed succssefully

            // parse output object
            let disConnRes = output;

            if (disConnRes != 1) {
                // disconnect error
                reject(new Error(
                    "Data mismatch in PLC connection pools.\n" +
                    "The application has attempted to terminate a PLC connection that the S7CommPlusDriverManger does not recognize"
                ));
                return;
            } // else
            // disconnect successful

            // save ipAddress: sessionID2 key-val pair for return before forgetting connection
            result = {
                ipAddress: ipAddress, 
                sessID2: plcConns.get(ipAddress)
            }
            // forget PLC connection 
            plcConns.delete(ipAddress);
            resolve(result);
        });
    });
}
const MultiDisconnect = (ipArray) => {
    return new Promise((resolve, reject) => {

        let disConnResults = []
        async.each(ipArray, async function(ipAddress, callback) {
            try {
                // attempt disconnect 
                const disConnRes = await Disconnect(ipAddress);
                // store successful disconnect result
                disConnResults.push({
                    status: "Success",
                    IP: disConnRes.ipAddress,
                    SessID: disConnRes.sessID2
                });
                // proceed to next disconnect attempt
                callback();
            } catch (error) {
                // store unsuccessful disconnect attempt
                disConnResults.push({
                    status: "Success",
                    IP: ipAddress,
                    SessID: 0,
                    error: error
                });
                // proceed to next disconnect attempt
                callback();
            }
        }, function (err) {
            if (err) {
                reject(new Error(
                    "An Error has occured while attempting to disconnect from PLCs"
                ));
                return;
            }
            resolve(disConnResults);
        });
    });
}



var GetDataBlockInfoList_ = edge.func({
    assemblyFile: '.\\S7CommPlusDllWrapper\\bin\\x64\\Debug\\S7CommPlusDllWrapper.dll', 
    typeName: 'S7CommPlusDriverWrapper.DriverManager',
    methodName: 'GetDataBlockInfoList'
});
const GetDataBlockInfoList = (ipAddress) => {
    return new Promise((resolve, reject) => {

        // check for IP
        if ( !(plcConns.has(ipAddress)) ) {
            reject(new Error(
                "no such PLC connection @ IP: " + ipAddress + " exists"
            ));
            return;
        } // else
        // retrieve corresponding sessionID for IP
        let sessionID2 = plcConns.get(ipAddress);

        // init input object
        input = {
            sessionID2: sessionID2
        }
        GetDataBlockInfoList_(input, (error, output) => {
            if (error) {
                reject(error);
                return;
            } // else 
            // Datablock information retrieved successfully

            // parse output object
            let accessRes = output.Item1;
            let dbInfoList = output.Item2;

            if (accessRes != 0) {
                // Datablock Information List access unsuccessful
                reject(new Error(
                    "unable to retrieve Datablock Information List from PLC @ IP: " + ipAddress + " (access code: " + accessRes + ")"
                ));
                return;
            } // else
            // Datablock Information List access successful

            resolve({
                ipAddress: ipAddress,
                dbInfoList: JSON.stringify(dbInfoList, null, 2)
            });
        });
    });
};



var GetTags_ = edge.func({
    assemblyFile: '.\\S7CommPlusDllWrapper\\bin\\x64\\Debug\\S7CommPlusDllWrapper.dll', 
    typeName: 'S7CommPlusDriverWrapper.DriverManager',
    methodName: 'GetTags'
});
const GetTags = (ipAddress, tagSymbolList) => {
    return new Promise((resolve,reject) => {

        // check for IP
        if ( !(plcConns.has(ipAddress)) ) {
            reject(new Error(
                "no such PLC connection @ IP: " + ipAddress + " exists"
            ));
            return;
        } // else
        // retrieve corresponding sessionID for IP
        let sessionID2 = plcConns.get(ipAddress);

        // init input object
        input = {
            sessionID2: sessionID2,
            tagSymbols: tagSymbolList
        }
        GetTags_(input, (error, output) => {
            if (error) {
                reject(error);
                return;
            } // else 
            // GetTags executed successfully

            // parse output object
            let accessRes = output.Item1;
            let readTags = output.Item2;

            if (accessRes != 0) {
                // Reading Tag values was unsuccessful
                reject(new Error(
                    "unable to retrieve Tag values from PLC @ IP: " + ipAddress + " (access code: " + accessRes + ")"
                ));
                return;
            } // else
            // Datablock Information List access successful
    
            resolve({
                ipAddress: ipAddress,
                readTags: JSON.stringify(readTags, null, 2)
            });
        });
    });
}




async function main() {

    targetPlcIP = "192.168.18.26"
    
    try {
        console.log("\n=== Testing Connect ===\n");
        console.log("Connecting to PLC @ IP: " + targetPlcIP)
        result = await Connect(targetPlcIP, "", 5000);
        console.log("successful connection to PLC @ IP: " + result.ipAddress + " (SessionID: " + result.sessID2 + ")");
        console.log("\n====================================\n");

        /*console.log("\n=== Testing GetDataBlockInfoList ===\n");
        result = await GetDataBlockInfoList(targetPlcIP);
        console.log("successfully retrieved Datablock Information List from PLC @ IP: " + result.ipAddress);
        console.log("Datablock Information List: \n" + result.dbInfoList);
        console.log("\n====================================\n");*/

        console.log("\n=== Testing GetTags ===\n");
        const tagSymbols = ["Test.Start", "Test.Value1", "Test.Value2"];
        result = await GetTags(targetPlcIP, tagSymbols);
        console.log("successfully retrieved Tag values from PLC @ IP: " + result.ipAddress);
        console.log("Read Tag values: \n" + result.readTags);
        console.log("\n====================================\n");


        //await new Promise(resolve => setTimeout(resolve, 5000));   

        console.log("\n=== Testing Disconnect ===\n");
        console.log("Disconnecting from PLC @ IP : " + targetPlcIP);
        result = await Disconnect(targetPlcIP); 
        console.log("successful disconnect from PLC @ IP: " + result.ipAddress + " (SessionID: " + result.sessID2 + ")");
        
        console.log("\n====================================\n");

    } catch (error) {
        console.log("Error:\n >>>", error.message);
    }
}
main();









/*


function isValidIPv4(ip) {
    const ipv4Regex = /^(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$/;
    return ipv4Regex.test(ip);
}
const rl_interface = readline.createInterface({
        input: process.stdin,
        output: process.stdout
    });

    let targetPlcIP = ""
    rl_interface.question("Enter PLC IP address:\n >", (userInput) => {
        
    });

    // prompt user to enter valid IP address
    
    let userInputValid = false
    while (!userInputValid) {
        let userInput = prompt();
        if (isValidIPv4(userInput)) {
            targetPlcIP = userInput;
            userInputValid = true;
            break;
        }
        console.log("Invalid IP address entered\n");
    }
    // user has entered valid IP address*/





/*var GetDataBlockInfoList_ = edge.func({
    assemblyFile: '.\\S7CommPlusDllWrapper\\bin\\x64\\Debug\\S7CommPlusDllWrapper.dll', 
    typeName: 'S7CommPlusDriverWrapper.DriverManager',
    methodName: 'GetDataBlockInfoList'
});



async function main() {
    try {
        console.log("\n====================================\n");

        const connParams = {
            ipAddress: "192.168.18.25",
            password: "", 
            timeout: 5000
        } 
        const connRes = await Connect(connParams);  // Connect is now a Promise-based function
        console.log("Connected successfully!");
        console.log("Connection Result: " + connRes);

        console.log("\n====================================\n");

        const dataBlockInfo = await GetDataBlockInfoList();
        console.log("Data Block Information successfully retrieved");
        console.log("Data Block Info:", dataBlockInfo);

        console.log("\n====================================\n");

    } catch (error) {
        console.log("Error:", error.message);
    }
}
main();
*/








/*const edge = require('edge-js');



var Connect = edge.func({
    assemblyFile: '.\\S7CommPlusDllWrapper\\bin\\x64\\Debug\\S7CommPlusDllWrapper.dll', 
    typeName: 'S7CommPlusDriverWrapper.DriverManager',
    methodName: 'Connect'
});

var GetDataBlockInfoList = edge.func({
    assemblyFile: '.\\S7CommPlusDllWrapper\\bin\\x64\\Debug\\S7CommPlusDllWrapper.dll', 
    typeName: 'S7CommPlusDriverWrapper.DriverManager',
    methodName: 'GetDataBlockInfoList'
});

var GetPObject_atDBInfoListIndex = edge.func({
    assemblyFile: '.\\S7CommPlusDllWrapper\\bin\\x64\\Debug\\S7CommPlusDllWrapper.dll', 
    typeName: 'S7CommPlusDriverWrapper.DriverManager',
    methodName: 'GetPObject_atDBInfoListIndex'
});

var GetDataBlockVariables_atDBInfoListIndex = edge.func({
    assemblyFile: '.\\S7CommPlusDllWrapper\\bin\\x64\\Debug\\S7CommPlusDllWrapper.dll', 
    typeName: 'S7CommPlusDriverWrapper.DriverManager',
    methodName: 'GetDataBlockVariables_atDBInfoListIndex'
});

var GetDataBlockNamesandVariables = edge.func({
    assemblyFile: '.\\S7CommPlusDllWrapper\\bin\\x64\\Debug\\S7CommPlusDllWrapper.dll', 
    typeName: 'S7CommPlusDriverWrapper.DriverManager',
    methodName: 'GetDataBlockNamesandVariables'
});


var GetTag = edge.func({
    assemblyFile: '.\\S7CommPlusDllWrapper\\bin\\x64\\Debug\\S7CommPlusDllWrapper.dll', 
    typeName: 'S7CommPlusDriverWrapper.DriverManager',
    methodName: 'GetTag'
});

var Disconnect = edge.func({
    assemblyFile: '.\\S7CommPlusDllWrapper\\bin\\x64\\Debug\\S7CommPlusDllWrapper.dll', 
    typeName: 'S7CommPlusDriverWrapper.DriverManager',
    methodName: 'Disconnect'
});


const connParams = {
    ipAddress: "192.168.18.25",
    password: "", 
    timeout: 5000
}
Connect(connParams, (error, result) => {
    console.log("Establishing connection to PLC");
    if (error) {
        console.log("Error connecting to PLC: ", error);
        return;
    }
    console.log(result);
});

console.log("\n====================================\n");

GetDataBlockInfoList(null, (error, result) => {
    if (error) {
        console.log("Error connecting to PLC: ", error);
        return;
    }
    console.log(result);
});


console.log("\n====================================\n");

GetPObject_atDBInfoListIndex({index: 1}, (error, result) => {
    if (error) {
        console.log("Error connecting to PLC: ", error);
        return;
    }
    console.log(result); 
});

console.log("\n====================================\n");

GetDataBlockVariables_atDBInfoListIndex({index: 1}, (error, result) => {
    if (error) {
        console.log("Error: ", error);
        return;
    }
    console.log(result);
});

console.log("\n====================================\n");

GetDataBlockNamesandVariables(null, (error, result) => {
    if (error) {
        console.log("Error: ", error);
        return;
    }
    console.log(result);
});

console.log("\n====================================\n");



console.log("\n====================================\n");

console.log("Disconnecting from PLC");
Disconnect(null, (disconnectError, disconnectResult) => {
    if (disconnectError) {
        console.error('Error disconnecting from PLC:', disconnectError);
    } else {
        console.log(disconnectResult);
    }
});*/