
type Theme =
    | 'Light'
    | 'Dark';

type Note = {
    id: string;
    content: string;
    rev: string;
};

type List = {
    id: string;
    rev: string;
};

type ListItem = {
    id: string;
    isDone: boolean;
    listId: string;
    name: string;
    rev: string;
};
declare interface Window {
    Mandadin: { Theme, Share, Clipboard, Database; };
}