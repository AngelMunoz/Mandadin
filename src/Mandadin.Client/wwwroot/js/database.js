
const notes = new PouchDB("notas");

/**
 * 
 * @param {Doc<Note>} doc 
 * @returns {Note}
 */
function mapDocument(doc) {
  console.log(doc);
  return {
    id: doc._id,
    rev: doc._rev,
    content: doc.content
  }
}

/**
 * 
 * @param {AllDocsResponse} docsResponse
 * @returns {Note[]}
 */
function mapAllDocs({ total_rows, offset, rows }) {
  console.log({ total_rows, offset });
  console.table(rows);
  return rows.map(({ id, doc }) => (
    {
      id: id,
      rev: doc._rev,
      content: doc.content
    }
  ));
}

/**
 * @returns {Promise<Note[]>}
 */
export async function FindNotes() {
  let result = await notes.allDocs({ include_docs: true }).then(mapAllDocs);
  console.log(result);
  return result;
}

/**
 * 
 * @param {string} Content
 * @returns {Promise<Note>}
 */
export async function CreateNote(content) {
  const note = { content, _id: `${Date.now()}` }
  /**
   * @type {DocumentOperationResult}
   */
  const result = await notes.put(note);
  console.log(result);
  return { id: result.id, content, rev: result.rev };
}

/**
 * 
 * @param {Note} note 
 * @return {Promise<Note>}
 */
export async function UpdateNote(note) {
  const toUpdate = { _id: note.id, _rev: note.rev, ...note, id: undefined, rev: undefined };
  /**
   * @type {DocumentOperationResult}
   */
  const result = await notes.put(toUpdate);
  return { ...note, rev: result.rev }
}

/**
 * 
 * @param {string} noteid 
 * @returns {Promise<Note>}
 */
export function FindNote(noteid) {
  return notes.get(noteid).then(mapDocument);
}

/**
 * 
 * @param {string} noteid 
 * @param {string} rev 
 * @returns {Promise<[string, string]>}
 */
export async function DeleteNote(noteid, noterev) {
  const { id, ok, rev } = await notes.remove(noteid, noterev);
  if (ok) {
    return [id, rev];
  }
  throw new Error(`Failed to delete document with id: [${id}]`);
}