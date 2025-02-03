
const edge = require('edge-js');

var Connect_ = edge.func({
    assemblyFile: '.\\S7CommPlusDllWrapper\\bin\\x64\\Debug\\S7CommPlusDllWrapper.dll', 
    typeName: 'S7CommPlusDriverWrapper.DriverManager',
    methodName: 'Connect'
});
const Connect = (params) => {
    return new Promise((resolve, reject) => {
        Connect_(params, (error, result) => {
            if (error) {
                reject(error);
                return;
            }
            if (result != 0) {
                reject(new Error(`Unable to connect: ${result}`));
                return;
            }
            params.connectionResult = result;
            resolve(JSON.stringify(params,null,2));
        });
    });
};

const ConnectToPLCs = (connParamsArray) => {
    return new Promise((resolve, reject) => {
        connParamsArray.forEach(connParams => {
            Connect(connParams)
        })
    });
}

