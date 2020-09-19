
const notes = new PouchDB("notas");
const lists = new PouchDB("lists");
const listItems = new PouchDB("listItems");


(async function(listItems) {
  try {
    const listIdIndex = listItems.createIndex({
      index: {
        fields: ['listId'],
        name: 'listIdIndex',
        ddoc: 'mandadinddoclistid',
      }
    });
    const isDoneIndex = listItems.createIndex({
      index: {
        fields: ['isDone', 'listId'],
        name: 'isDoneIndex',
        ddoc: 'mandadinddocisdone',
      }
    });
    const createIndexesResult = await Promise.all([listIdIndex, isDoneIndex]);
    console.log({ createIndexesResult });
  } catch (error) {
    console.warn(`Error creating index for ListItems [${error.message}]`);
  }
})(listItems)


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

/**
 * @returns {Promise<List[]>}
 */
export async function FindLists() {
  let result = await lists
    .allDocs({ include_docs: true }).
    then(({ total_rows, offset, rows }) => {
      console.log({ total_rows, offset });
      console.table(rows);
      return rows.map(({ id, doc }) => (
        {
          id: id,
          rev: doc._rev
        }
      ));
    });
  console.log(result);
  return result;
}

/**
 * 
 * @param {string} name 
 * @return {Promise<List>}
 */
function FindList(name) {
  return lists.get(name).then(doc => ({
    id: doc._id,
    rev: doc._rev
  }));
}

/**
 * 
 * @param {string} name 
 * @returns {Promise<boolean>}
 */
export async function ListNameExists(name) {
  try {
    const result = await FindList(name);
    console.log(result);
    return true;
  } catch (error) {
    console.log(error);
    return error.status === 404 ? false : true;
  }
}

/**
 * 
 * @param {string} name 
 * @returns {Promise<List>}
 */
export function CreateList(name) {
  return lists.put({ _id: name })
    .then(result => ({ id: result.id, rev: result.rev }));
}

async function DeleteAllListItemsFromList(listId) {
  try {
    const queryAllResult = await listItems.find({
      selector: { listId },
      use_index: '_design/mandadinddoclistid'
    })
    console.log({ queryAllResult });
    if (queryAllResult.docs && queryAllResult.docs.length > 0) {
      const docs = queryAllResult.docs.map(doc => ({ ...doc, _deleted: true }));
      const deleteResult = await listItems.bulkDocs(docs)
      console.log({ deleteResult });
    }
    return true;
  } catch (error) {
    console.warn({ error });
    return Promise.reject(new Error('Failed to Delete All Documents For List'));
  }
}

export async function DeleteList(listId, rev) {
  try {
    await DeleteAllListItemsFromList(listId)
    const deleteResult = await lists.remove(listId, rev)
    console.log({ deleteResult });
  } catch (error) {
    return Promise.reject(error);
  }
}
