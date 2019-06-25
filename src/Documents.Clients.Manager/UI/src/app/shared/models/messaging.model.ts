
export interface IMessaging{
    title: string;
    body: string;
    type?: 'alert' | 'toastr' | 'dialog'; // defaults to alert (inline)
    // Will add more such as icons, class, dialog etc
}

