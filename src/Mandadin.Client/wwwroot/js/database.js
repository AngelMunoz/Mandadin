
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
        fields: ['listId', 'isDone'],
        name: 'isDoneIndex',
        ddoc: 'mandadinddocisdone',
      }
    });
    const listItemNameIndex = listItems.createIndex({
      index: {
        fields: ['listId', 'name'],
        name: 'listItemNameIndex',
        ddoc: 'mandadinddoclistitemnameindex',
      },
      sort: ['name']
    });
    const createIndexesResult = await Promise.all([listIdIndex, isDoneIndex, listItemNameIndex]);
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
export function FindNotes() {
  return notes.allDocs({ include_docs: true })
    .then(mapAllDocs)
    .then(findNotesResult => {
      console.log({ result: findNotesResult });
      return findNotesResult
    });
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
  const createNoteResult = await notes.put(note);
  console.log({ createNoteResult });
  return { id: createNoteResult.id, content, rev: createNoteResult.rev };
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
  const updateNoteResult = await notes.put(toUpdate);
  return { ...note, rev: updateNoteResult.rev }
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
  try {
    const { id, ok, rev } = await notes.remove(noteid, noterev);
    if (!ok) {
      return Promise.reject(`Failed to delete document with id: [${id}]`);
    }
    return [id, rev];
  } catch (deleteNoteError) {
    console.warn({ deleteNoteError });
    return Promise.reject(deleteNoteError.message);
  }
}

/**
 * @returns {Promise<List[]>}
 */
export async function FindLists() {
  return lists
    .allDocs({ include_docs: true }).
    then(({ total_rows, offset, rows }) => {
      return rows.map(({ id, doc }) => (
        {
          id: id,
          rev: doc._rev
        }
      ));
    })
    .then(findListResults => {
      console.log({ findListResults });
      return findListResults;
    });
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
    const listNameExistsResult = await FindList(name);
    console.log({ listNameExistsResult });
    return true;
  } catch (listNameExistsError) {
    if (listNameExistsError.status === 404) {
      return false;
    }
    console.warn({ listNameExistsError });
    return true;
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
    console.warn({ DeleteAllListItemsFromListError: error });
    return Promise.reject('Failed to Delete All Documents For List');
  }
}

export async function DeleteList(listId, rev) {
  try {
    await DeleteAllListItemsFromList(listId)
    const deleteResult = await lists.remove(listId, rev)
    console.log({ deleteResult });
  } catch (deleteListError) {
    console.warn({ deleteListError })
    return Promise.reject(deleteListError.message);
  }
}

function buildIndexQuery(listId, hideDone) {
  const selector =
    hideDone === false ? { listId } : { listId, hideDone: false }

  return {
    fields: ['_id', '_rev', 'listId', 'isDone', 'name'],
    use_index: `_design/${hideDone ? 'mandadinddoclistid' : 'mandadinddocisdone'}`,
    selector,
  }
}


/**
 * 
 * @param {string} listId
 * @param {bool} hideDone 
 */
export async function GetListItems(listId, hideDone) {
  try {
    const index = buildIndexQuery(listId, hideDone)
    const { docs } = await listItems.find(index);
    return docs.map(({ _id, _rev, listId, isDone, name }) =>
      ({ id: _id, rev: _rev, listId, isDone, name }));
  } catch (getListItemsError) {
    console.warn({ getListItemsError })
    return Promise.reject(getListItemsError.message);
  }
}
/**
 * 
 * @param {string} name 
 * @returns {Promise<boolean>}
 */
export function ListItemExists(listId, name) {
  return listItems.find({
    selector: { listId, name },
    fields: ['name'],
    use_index: '_design/mandadinddoclistitemnameindex'
  })
    .then(({ docs }) => docs.length > 0)
    .catch(listItemExistsError => {
      console.log({ listItemExistsError });
      return Promise.reject(listItemExistsError.message);
    });
}

export async function CreateListItem(listId, name) {
  try {
    const { id, ok } = await listItems.put({
      isDone: false,
      _id: `${listId}:${Date.now()}`,
      name,
      listId
    });
    if (!ok) { return Promise.reject('Could not create document'); }
    const { _id, isDone, _rev, ...props } = await listItems.get(id);
    return { id: _id, rev: _rev, listId: props.listId, name: props.name, isDone };
  } catch (createListItemError) {
    console.warn({ createListItemError })
    return Promise.reject(createListItemError.message);
  }
}

/**
 * 
 * @param {ListItem} item
 * @return {Promise<ListItem>}
 */
export async function UpdateListItem(item) {
  try {
    const { id, rev, ...itemProps } = item
    const { ok, ...result } = await listItems.put({ _id: id, _rev: rev, ...itemProps })
    if (!ok) { return Promise.reject('Failed to update ListItem'); }
    return { ...item, ...result };
  } catch (updateListItemError) {
    console.warn({ updateListItemError });
    return Promise.reject(updateListItemError.message);
  }
}

/**
 * updates the document with the property `_deleted: true` to enable undo actions
 * @param {ListItem} item
 * @return {Promise<ListItem>}
 */
export function DeleteListItem(item) {
  return UpdateListItem({ ...item, _deleted: true })
    .then(item => ({ ...item, _deleted: undefined }));
}