
const edge = require('edge-js');



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
                sessID2: sessID2}
            );
        });
    });
};

var Disconnect_ = edge.func({
    assemblyFile: '.\\S7CommPlusDllWrapper\\bin\\x64\\Debug\\S7CommPlusDllWrapper.dll', 
    typeName: 'S7CommPlusDriverWrapper.DriverManager',
    methodName: 'Disconnect'
});
const Disconnect = (ipAddress) => {
    return new Promise((resolve, reject) => {

        // check for IP
        if ( !(plcConns.has(ipAddress)) ) {
            console.log("no such PLC connection @ IP: " + ipAddress + " exists");
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



async function main() {
    try {
        console.log("\n=== Testing Connect ===\n");
        console.log("Connecting to PLC @ IP 192.168.18.25")

        result = await Connect("192.168.18.25", "", 5000);
        console.log("successful connection to PLC @ IP: " + result.ipAddress + " (SessionID: " + result.sessID2 + ")");
        console.log("\n====================================\n");

        await new Promise(resolve => setTimeout(resolve, 5000));   

        console.log("\n=== Testing Disconnect ===\n");
        console.log("Disconnecting from PLC @ IP 192.168.18.25")
        result = await Disconnect("192.168.18.25") 
        console.log("successful disconnect from PLC @ IP: " + result.ipAddress + " (SessionID: " + result.sessID2 + ")");
        
        console.log("\n====================================\n");

    } catch (error) {
        console.log("Error:\n >>>", error.message);
    }
}
main();















/*var GetDataBlockInfoList_ = edge.func({
    assemblyFile: '.\\S7CommPlusDllWrapper\\bin\\x64\\Debug\\S7CommPlusDllWrapper.dll', 
    typeName: 'S7CommPlusDriverWrapper.DriverManager',
    methodName: 'GetDataBlockInfoList'
});
const GetDataBlockInfoList = () => {
    return new Promise((resolve, reject) => {
        GetDataBlockInfoList_(null, (error, result) => {
            if (error) {
                reject(error);
                return;
            }
            resolve(result);
        });
    });
};


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

GetTag({tagSymbol: "DB_CharacterStrings.String_Hello_World"}, (error, result) => {
    if (error) {
        console.log("Error connecting to PLC: ", error);
        return;
    }
    console.log(result);
});

console.log("\n====================================\n");

console.log("Disconnecting from PLC");
Disconnect(null, (disconnectError, disconnectResult) => {
    if (disconnectError) {
        console.error('Error disconnecting from PLC:', disconnectError);
    } else {
        console.log(disconnectResult);
    }
});*/