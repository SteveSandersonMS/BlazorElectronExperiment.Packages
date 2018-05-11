const { app } = require('electron');
const fs = require('fs');
const path = require('path');
const process = require('process');

let io, browserWindows, ipc, apiProcess, loadURL;
let appApi, menu, dialog, notification, tray, webContents;
let globalShortcut, shell, screen, clipboard;

let port = process.argv[2];
app.on('ready', () => {
  startSocketApiBridge(port);
});

function startSocketApiBridge(port) {
    // TODO: Don't use TCP as the transport:
    // [1] It's messy that it displays the Windows "can this process listen?" dialog
    // [2] It's not doing anything to prevent other processes from connecting and then
    //     running operations in the security context of the Electron process
    io = require('socket.io')(port);
    console.log('socket.io is listening on port ' + port);

    io.on('connection', (socket) => {
        console.log('.NET Core Application connected...');
        
        appApi = require('./api/app')(socket, app);
        browserWindows = require('./api/browserWindows')(socket);
        ipc = require('./api/ipc')(socket);
        menu = require('./api/menu')(socket);
        dialog = require('./api/dialog')(socket);
        notification = require('./api/notification')(socket);
        tray = require('./api/tray')(socket);
        webContents = require('./api/webContents')(socket);
        globalShortcut = require('./api/globalShortcut')(socket);
        shell = require('./api/shell')(socket);
        screen = require('./api/screen')(socket);
        clipboard = require('./api/clipboard')(socket);
    });
}