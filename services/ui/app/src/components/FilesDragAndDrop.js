import React, { useEffect, useRef, useState } from 'react';

import './FilesDragAndDrop.scss';

const FilesDragAndDrop = ({ onDrop, children }) => {
  const drop = useRef(null);
  const [dragging, setDragging] = useState(false);
  const drag = useRef(null);

  useEffect(() => {
    drop.current.addEventListener('dragover', handleDragOver);
    drop.current.addEventListener('drop', handleDrop);
    drop.current.addEventListener('dragenter', handleDragEnter);
    drop.current.addEventListener('dragleave', handleDragLeave);

    return () => {
      drop.current.removeEventListener('dragover', handleDragOver);
      drop.current.removeEventListener('drop', handleDrop);
      drop.current.removeEventListener('dragenter', handleDragEnter);
      drop.current.removeEventListener('dragleave', handleDragLeave);
    };
  }, []);

  const handleDragOver = e => {
    e.preventDefault();
    e.stopPropagation();
  };

  const handleDrop = e => {
    e.preventDefault();
    e.stopPropagation();

    const { files } = e.dataTransfer;

    if (files && files.length) {
      onDrop(files);
    }
  };

  const handleDragEnter = e => {
    e.preventDefault();
    e.stopPropagation();

    if (e.target !== drag.current) {
      setDragging(true);
    }
  };

  const handleDragLeave = e => {
    e.preventDefault();
    e.stopPropagation();

    if (e.target === drag.current) {
      setDragging(false);
    }
  };

  return (
    <div ref={drop} className="FilesDragAndDrop">
      {dragging && (
        <div className="FilesDragAndDrop__placeholder" ref={drag}></div>
      )}
      {children}
    </div>
  );
};

export default FilesDragAndDrop;
