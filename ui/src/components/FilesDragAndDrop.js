import React, { useEffect, useRef, useState } from 'react';
import PropTypes from 'prop-types';

import './FilesDragAndDrop.scss';

export default function FilesDragAndDrop({ onUpload }) {
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
      onUpload(files);
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
        <div className="FilesDragAndDrop__placeholder" ref={drag}>
          Drop that file down low
          <span role="img" aria-label="emoji" className="area__icon">
            &#128526;
          </span>
        </div>
      )}
      <div className="FilesDragAndDrop__area">
        Hey, drop me some files
        <span role="img" aria-label="emoji" className="area__icon">
          &#128526;
        </span>
      </div>
    </div>
  );
}

FilesDragAndDrop.propTypes = {
  onUpload: PropTypes.func.isRequired,
};
