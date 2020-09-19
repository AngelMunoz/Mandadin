
type Theme =
    | 'Light'
    | 'Dark'
// Enable Custom Theme Later
// | ['Custom', string[]]

type Attachment = {
    content_type: string
    digest: string
    data?: string
    stub?: boolean
}

type Doc<T> = {
    _id: string
    _rev: string
    _attachments?: {
        [key: string]: Attachment
    }
    [P: keyof T]: T[P]
}

type DocumentOperationResult = {
    ok: boolean
    id: string
    rev: string
}

type RowDefinition<T> = {
    id: string
    key: string
    value: { rev: string }
    doc: Doc<T>[]
}

type AllDocsResponse = {
    offset: number
    total_rows: number
    rows: RowDefinition
}

type Note = {
    id: string
    content: string
    rev: string
}