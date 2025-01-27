const edge = require('edge-js');

var Connect = edge.func({
    assemblyFile: '.\\S7CommPlusDllWrapper\\bin\\x64\\Debug\\S7CommPlusDllWrapper.dll', 
    typeName: 'S7CommPlusDriverWrapper.DriverManager',
    methodName: 'Connect'
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

Connect(connParams, (error, connRes) => {
    console.log("Establishing connection to PLC");
    if (error) {
        console.log("Error connecting to PLC: ", error);
        return;
    }
    if (connRes != 0) {
        console.log("Unable to connect to PLC (connection code: ", connRes, ")");
        return;
    }
    console.log("Successful connection to PLC (connection code: ", connRes, ")");

    // Disconnect after connecting
    console.log("Disconnecting from PLC");
    Disconnect(null, (disconnectError, disconnectResult) => {
        if (disconnectError) {
          console.error('Error disconnecting from PLC:', disconnectError);
        } else {
          console.log(disconnectResult);
        }
      });
});
